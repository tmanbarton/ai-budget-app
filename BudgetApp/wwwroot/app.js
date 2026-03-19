const statusElement = document.getElementById("status");

function showStatus(message, type) {
    statusElement.textContent = message;
    statusElement.className = type;
}

document.getElementById("date").valueAsDate = new Date();

async function submitAiInput() {
    const aiInput = document.getElementById("ai-input");
    const text = aiInput.value.trim();
    if (!text) return;

    showStatus("Parsing...", "success");

    try {
        const response = await fetch("/transaction/parse", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ text })
        });

        const result = await response.json();

        if (!response.ok) {
            showStatus("Failed to parse transaction.", "error");
            return;
        }

        if (result.needsClarification) {
            showStatus(result.needsClarification, "info");
            return;
        }

        aiInput.value = "";
        showStatus(`Transaction for $${result.amount} in ${result.category} on ${result.date} added successfully.`, "success");
        await refreshChart();
    } catch {
        showStatus("Something went wrong.", "error");
    }
}

document.getElementById("ai-submit").addEventListener("click", submitAiInput);
document.getElementById("ai-input").addEventListener("keydown", (e) => {
    if (e.key === "Enter") submitAiInput();
});

const amountInput = document.getElementById("amount");
amountInput.addEventListener("input", () => {
    if (amountInput.validity.rangeUnderflow) {
        amountInput.setCustomValidity("Think positively. The amount must be greater than zero.");
    } else {
        amountInput.setCustomValidity("");
    }
});

async function refreshChart() {
    const response = await fetch(`/transactions`);
    const data = await response.json();

    // Group by month and category
    const monthCategory = {};
    const categories = new Set();
    data.forEach(t => {
        const month = t.date.slice(0, 7);
        if (!monthCategory[month]) monthCategory[month] = {};
        monthCategory[month][t.category] = (monthCategory[month][t.category] || 0) + t.amount;
        categories.add(t.category);
    });

    const sortedMonths = Object.keys(monthCategory).sort();

    // One dataset per category
    const datasets = [...categories].map(cat => ({
        label: cat,
        data: sortedMonths.map(m => monthCategory[m][cat] || 0)
    }));

    if (window.myChart) window.myChart.destroy();

    window.myChart = new Chart(document.getElementById("chart"), {
        type: "bar",
        data: {
            labels: sortedMonths,
            datasets: datasets
        },
        options: {
            scales: {
                x: { stacked: true },
                y: { stacked: true, beginAtZero: true }
            }
        }
    });
}

document.getElementById("transaction-form").addEventListener("submit", async (e) => {
    e.preventDefault();

    const amount = parseFloat(document.getElementById("amount").value);
    const date = document.getElementById("date").value;
    const category = document.getElementById("category").value;

    const selectedDate = new Date(date + "T00:00:00");
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const daysDiff = Math.round((selectedDate - today) / (1000 * 60 * 60 * 24));
    if (daysDiff > 30 && !confirm(`The date is ${daysDiff} days in the future. Are you sure that's correct?`)) {
        return;
    }

    const response = await fetch(`/transaction`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ amount, date, category })
    });

    if (response.ok) {
        e.target.reset();
        showStatus("Transaction added.", "success");
        await refreshChart();
    } else {
        const error = await response.text();
        showStatus(error || "Failed to add transaction.", "error");
    }
});

refreshChart();