document.getElementById("loadDocuments").addEventListener("click", async () => {
    const list = document.getElementById("docList");
    list.innerHTML = "Loading...";

    try {
        const response = await fetch("/api/Document/e988b34f-0f09-4d0e-a1ee-23ce2784c1c4"); // upload document on swagger and enter own id here for testing
        if (!response.ok) throw new Error(`Server error: ${response.status}`);

        const doc = await response.json();
        list.innerHTML = "";
        const li = document.createElement("li");
        li.textContent = `${doc.fileName} (${doc.id})`;
        list.appendChild(li);
    } catch (err) {
        list.innerHTML = `<li style="color:red;">${err.message}</li>`;
    }
});
