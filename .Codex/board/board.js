const lanes = [
  { id: "todo", title: "Todo" },
  { id: "doing", title: "Doing" },
  { id: "review", title: "Review" },
  { id: "done", title: "Done" },
  { id: "blocked", title: "Blocked" },
];

const statusLabels = {
  todo: "未开始",
  doing: "进行中",
  review: "待验收",
  done: "完成",
  blocked: "阻塞",
  dropped: "放弃",
};

let plans = [];
let activeFilter = "all";
let activeRef = null;

const board = document.querySelector("#board");
const summary = document.querySelector("#summary");
const detailPanel = document.querySelector("#detailPanel");
const closeDetail = document.querySelector("#closeDetail");
const detailType = document.querySelector("#detailType");
const detailTitle = document.querySelector("#detailTitle");
const detailDescription = document.querySelector("#detailDescription");
const detailMeta = document.querySelector("#detailMeta");
const detailLink = document.querySelector("#detailLink");

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function flattenNodes() {
  return plans.flatMap((plan) =>
    (plan.nodes || []).map((node) => ({
      plan,
      node,
    })),
  );
}

function visibleNodes() {
  const items = flattenNodes();
  if (activeFilter === "all") {
    return items;
  }
  return items.filter((item) => item.node.status === activeFilter);
}

async function loadPlans() {
  const response = await fetch("/api/plans");
  if (!response.ok) {
    throw new Error(`读取 plans 失败：${response.status}`);
  }
  const payload = await response.json();
  plans = payload.plans || [];
  render();
}

function render() {
  renderSummary();
  renderBoard();
  if (activeRef) {
    const found = flattenNodes().find(
      (item) => item.plan.id === activeRef.planId && item.node.id === activeRef.nodeId,
    );
    if (found) {
      openDetail(found.plan.id, found.node.id);
    }
  }
}

function renderSummary() {
  const items = flattenNodes();
  summary.innerHTML = lanes
    .map((lane) => {
      const count = items.filter((item) => item.node.status === lane.id).length;
      return `
        <div class="summary-pill">
          <strong>${count}</strong>
          <span>${lane.title}</span>
        </div>
      `;
    })
    .join("");
}

function renderBoard() {
  const nodes = visibleNodes();
  board.innerHTML = lanes
    .map((lane) => {
      const laneNodes = nodes.filter((item) => item.node.status === lane.id);
      const cards = laneNodes.map(renderCard).join("");
      return `
        <article class="lane">
          <header class="lane-header">
            <h2 class="lane-title">${lane.title}</h2>
            <span class="lane-count">${laneNodes.length}</span>
          </header>
          <div class="cards">${cards}</div>
        </article>
      `;
    })
    .join("");

  document.querySelectorAll(".task-card").forEach((card) => {
    card.addEventListener("click", () => openDetail(card.dataset.planId, card.dataset.nodeId));
  });
}

function renderCard({ plan, node }) {
  return `
    <button
      class="task-card"
      type="button"
      data-plan-id="${escapeHtml(plan.id)}"
      data-node-id="${escapeHtml(node.id)}"
      data-kind="${escapeHtml(node.kind || "node")}"
    >
      <div class="tag-row">
        <span class="tag">${escapeHtml(node.kind || "node")}</span>
        <span class="tag">${escapeHtml(statusLabels[node.status] || node.status)}</span>
      </div>
      <h3>${escapeHtml(node.title)}</h3>
      <p>${escapeHtml(node.description || plan.title)}</p>
      <small>${escapeHtml(plan.title)}</small>
    </button>
  `;
}

function findItem(planId, nodeId) {
  return flattenNodes().find((item) => item.plan.id === planId && item.node.id === nodeId);
}

function openDetail(planId, nodeId) {
  const item = findItem(planId, nodeId);
  if (!item) {
    return;
  }
  const { plan, node } = item;
  activeRef = { planId, nodeId };
  detailType.textContent = `${plan.title} / ${statusLabels[node.status] || node.status}`;
  detailTitle.textContent = node.title;
  detailDescription.textContent = node.description || "";
  detailMeta.innerHTML = `
    <dt>Plan</dt>
    <dd>${escapeHtml(plan.id)}</dd>
    <dt>Workflow</dt>
    <dd>${escapeHtml(plan.workflow || "-")}</dd>
    <dt>Harness</dt>
    <dd>${escapeHtml(node.harness || "-")}</dd>
    <dt>Artifacts</dt>
    <dd>${renderArtifacts(node.artifacts || [])}</dd>
    <dt>状态</dt>
    <dd>${renderStatusButtons(plan.id, node.id, node.status)}</dd>
    <dt>Actions</dt>
    <dd>${renderActions(plan.id, node)}</dd>
    <dt>Last Run</dt>
    <dd>${renderLastRun(node.last_run)}</dd>
  `;
  detailLink.href = `/api/plans/${encodeURIComponent(plan.id)}`;
  detailLink.textContent = "查看 Plan JSON";
  bindDetailActions();
  detailPanel.classList.add("is-open");
  detailPanel.setAttribute("aria-hidden", "false");
}

function renderArtifacts(artifacts) {
  if (!artifacts.length) {
    return "-";
  }
  return `
    <ul class="artifact-list">
      ${artifacts.map((item) => `<li>${escapeHtml(item)}</li>`).join("")}
    </ul>
  `;
}

function renderStatusButtons(planId, nodeId, currentStatus) {
  return `
    <div class="status-buttons">
      ${["todo", "doing", "review", "done", "blocked"]
        .map(
          (status) => `
            <button
              class="mini-button ${status === currentStatus ? "is-active" : ""}"
              type="button"
              data-status="${status}"
              data-plan-id="${escapeHtml(planId)}"
              data-node-id="${escapeHtml(nodeId)}"
            >${statusLabels[status]}</button>
          `,
        )
        .join("")}
    </div>
  `;
}

function renderActions(planId, node) {
  const actions = node.actions || [];
  if (!actions.length) {
    return "-";
  }
  return `
    <div class="action-buttons">
      ${actions
        .map(
          (action) => `
            <button
              class="run-button"
              type="button"
              data-action-id="${escapeHtml(action.id)}"
              data-plan-id="${escapeHtml(planId)}"
              data-node-id="${escapeHtml(node.id)}"
            >${escapeHtml(action.label || action.id)}</button>
          `,
        )
        .join("")}
    </div>
  `;
}

function renderLastRun(lastRun) {
  if (!lastRun) {
    return "-";
  }
  return `
    <span class="${lastRun.returncode === 0 ? "run-ok" : "run-fail"}">
      code ${escapeHtml(lastRun.returncode)}
    </span>
    <div>${escapeHtml(lastRun.finished_at || "")}</div>
    <div>${escapeHtml(lastRun.log || "")}</div>
  `;
}

function bindDetailActions() {
  document.querySelectorAll(".mini-button").forEach((button) => {
    button.addEventListener("click", async () => {
      await updateNodeStatus(button.dataset.planId, button.dataset.nodeId, button.dataset.status);
    });
  });

  document.querySelectorAll(".run-button").forEach((button) => {
    button.addEventListener("click", async () => {
      await runAction(button.dataset.planId, button.dataset.nodeId, button.dataset.actionId, button);
    });
  });
}

async function updateNodeStatus(planId, nodeId, status) {
  const response = await fetch(`/api/plans/${encodeURIComponent(planId)}/nodes/${encodeURIComponent(nodeId)}`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ status }),
  });
  if (!response.ok) {
    alert(`状态更新失败：${response.status}`);
    return;
  }
  const payload = await response.json();
  replacePlan(payload.plan);
  render();
}

async function runAction(planId, nodeId, actionId, button) {
  button.disabled = true;
  button.textContent = "执行中...";
  const response = await fetch("/api/actions/run", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ plan_id: planId, node_id: nodeId, action_id: actionId }),
  });
  const payload = await response.json();
  if (!response.ok) {
    alert(payload.error || `执行失败：${response.status}`);
    button.disabled = false;
    return;
  }
  replacePlan(payload.plan);
  render();
}

function replacePlan(plan) {
  const index = plans.findIndex((item) => item.id === plan.id);
  if (index >= 0) {
    plans[index] = plan;
  } else {
    plans.push(plan);
  }
}

function closePanel() {
  activeRef = null;
  detailPanel.classList.remove("is-open");
  detailPanel.setAttribute("aria-hidden", "true");
}

document.querySelectorAll(".filter").forEach((button) => {
  button.addEventListener("click", () => {
    activeFilter = button.dataset.filter;
    document.querySelectorAll(".filter").forEach((item) => item.classList.remove("is-active"));
    button.classList.add("is-active");
    closePanel();
    renderBoard();
  });
});

closeDetail.addEventListener("click", closePanel);
document.addEventListener("keydown", (event) => {
  if (event.key === "Escape") {
    closePanel();
  }
});

loadPlans().catch((error) => {
  board.innerHTML = `<p class="error-text">${escapeHtml(error.message)}</p>`;
});
