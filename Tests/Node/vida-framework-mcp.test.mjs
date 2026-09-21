import assert from "node:assert/strict";
import { mkdtemp, mkdir, readFile, readdir, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import test from "node:test";

const sourcePath = join(
  dirname(fileURLToPath(import.meta.url)),
  "..", "..", "Editor", "Resources", "VidaFrameworkCodex", "vida-framework-mcp.mjs.txt",
);

async function fixture({ signedIn = true, updatedAt = Date.now() } = {}) {
  const root = await mkdtemp(join(tmpdir(), "vida-framework-mcp-"));
  const project = join(root, "Project");
  const bridge = join(project, "Library", "VidaFramework", "CodexBridge");
  await mkdir(join(project, "Assets", "framework"), { recursive: true });
  await mkdir(join(project, "ProjectSettings"), { recursive: true });
  await mkdir(join(bridge, "requests"), { recursive: true });
  await mkdir(join(bridge, "responses"), { recursive: true });
  await writeFile(join(project, "ProjectSettings", "ProjectVersion.txt"), "m_EditorVersion: 6000.6.0f1\n");
  await writeFile(join(bridge, "status.json"), `${JSON.stringify({
    schemaVersion: "1.0",
    bridgeVersion: "1.0.0",
    ready: true,
    projectPath: project,
    processId: process.pid,
    unityVersion: "6000.6.0f1",
    frameworkVersion: "1.2.7",
    signedIn,
    busy: false,
    activeOperationId: "",
    updatedAt,
  })}\n`);
  const modulePath = join(root, "vida-framework-mcp.mjs");
  await writeFile(modulePath, await readFile(sourcePath, "utf8"));
  const client = await import(`${pathToFileURL(modulePath).href}?fixture=${encodeURIComponent(root)}`);
  return { root, project, bridge, client };
}

async function respondToNext(bridge, resultFactory) {
  const requests = join(bridge, "requests");
  const deadline = Date.now() + 3_000;
  while (Date.now() < deadline) {
    const names = (await readdir(requests)).filter(name => name.endsWith(".json"));
    if (names.length) {
      const request = JSON.parse(await readFile(join(requests, names[0]), "utf8"));
      const response = resultFactory(request);
      await writeFile(join(bridge, "responses", `${request.operationId}.json`), `${JSON.stringify(response)}\n`);
      return request;
    }
    await new Promise(resolvePromise => setTimeout(resolvePromise, 20));
  }
  throw new Error("bridge request was not created");
}

function completed(request, fields = {}) {
  return {
    schemaVersion: "1.0",
    operationId: request.operationId,
    command: request.command,
    success: true,
    state: "completed",
    completedAt: Date.now(),
    ...fields,
  };
}

test("status resolves a Unity root from a nested project path", async () => {
  const { project, client } = await fixture();
  const status = await client.callTool("vida_framework_status", {
    projectPath: join(project, "Assets", "framework"),
  });
  assert.equal(status.ready, true);
  assert.equal(status.signedIn, true);
  assert.equal(status.frameworkVersion, "1.2.7");
});

test("sign-in reuses an existing Unity Framework session without enqueuing a request", async () => {
  const { project, bridge, client } = await fixture();
  const result = await client.callTool("vida_framework_sign_in", { projectPath: project });
  assert.deepEqual(result, { success: true, state: "completed", signedIn: true, alreadySignedIn: true });
  assert.deepEqual(await readdir(join(bridge, "requests")), []);
});

test("package listing sends a bounded request and returns only the Unity response", async () => {
  const { project, bridge, client } = await fixture();
  const responder = respondToNext(bridge, request => completed(request, {
    signedIn: true,
    packages: [{ id: "vida-sdk", kind: "sdk", name: "Vida SDK", version: "2.0.0" }],
  }));
  const result = await client.callTool("vida_framework_list_packages", { projectPath: project, kind: "sdk" });
  const request = await responder;
  assert.equal(request.command, "list_packages");
  assert.equal(request.kind, "sdk");
  assert.deepEqual(result.packages.map(value => value.id), ["vida-sdk"]);
});

test("latest installation accepts an exact package ID and exposes a pollable operation", async () => {
  const { project, bridge, client } = await fixture();
  const responder = respondToNext(bridge, request => completed(request, {
    signedIn: true,
    packageId: request.packageId,
    version: "3.1.0",
  }));
  const result = await client.callTool("vida_framework_install_latest", {
    projectPath: project,
    packageId: "vida-starter",
  });
  const request = await responder;
  assert.equal(request.command, "install_latest");
  assert.equal(request.packageId, "vida-starter");
  assert.equal(result.version, "3.1.0");
});

test("Unity owns sign-in enforcement and stale Editors fail before requests", async () => {
  const unsigned = await fixture({ signedIn: false });
  const responder = respondToNext(unsigned.bridge, request => ({
    schemaVersion: "1.0",
    operationId: request.operationId,
    command: request.command,
    success: false,
    state: "failed",
    errorCode: "sign_in_required",
    errorMessage: "Call vida_framework_sign_in first.",
    completedAt: Date.now(),
  }));
  await assert.rejects(
    () => unsigned.client.callTool("vida_framework_list_packages", { projectPath: unsigned.project }),
    error => error.code === "sign_in_required",
  );
  const request = await responder;
  assert.equal(request.command, "list_packages");

  const stale = await fixture({ updatedAt: Date.now() - 60_000 });
  await assert.rejects(
    () => stale.client.callTool("vida_framework_status", { projectPath: stale.project }),
    error => error.code === "unity_editor_not_ready",
  );
});
