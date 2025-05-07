# pdfEngine demo wip
- RAG pipeline for pdf storage and realtime AI search & discussion tool

![image](https://github.com/user-attachments/assets/e18ab940-a5bf-4c32-b321-94b5950f56d7)



- Upload pdf documents and let it list the item
- Generates a resume which toggles on-click at sidebar on upload
- Drop all your PDF's in the designated area and let the AI go ham while the loading spinner mesmerizes 

![firefox_Oz8WByliLZ](https://github.com/user-attachments/assets/d4da0376-421a-402e-b4e7-7e69284d4e0f)


- AI Search uploaded document content through chat-window query
- Will generate GPT response to query and respond with top attached references (if any)
- Realtime chat history and auto query classifier means you can not only search the documents, but discuss the content outside of the uploaded material
- Super responsive UI & design by yours truly


# Stack
- ASP.NET WebAPI .NET 8 + Swagger
- Blazor .NET 8
- SQLite
- OpenAI embedding & chunking
- FAISS
- Vector search
- GPT-completion (3.5 turbo for demo, 4+ for eventual release)

# Run demo
- Requires active OpenAI API key, insert at pdfEngineAPI/appsettings.Development.json 
- Run ASP.NET migrations for EF/SQLite file persistance
- Demo configuration, will purposely exit and throw uncaught exception.
