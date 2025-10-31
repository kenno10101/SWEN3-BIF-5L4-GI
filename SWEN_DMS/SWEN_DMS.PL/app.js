document.getElementById("loadDocuments").addEventListener("click", async () => {
    const list = document.getElementById("docList");
    list.innerHTML = "Loading...";

    try {
        const response = await fetch("/api/Document/ee94be12-6947-4ff6-830d-00828036c889"); // upload document on swagger and enter own id here for testing
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
