# pdfEngine demo wip
- RAG pipeline for pdf storage and realtime AI search & discussion tool
- Realtime chat history & auto query classifier means you can discuss the uploaded material (Prioritizes search results if any)

![image](https://github.com/user-attachments/assets/e18ab940-a5bf-4c32-b321-94b5950f56d7)



- Upload single pdf document or bulk upload
- Will process, store & list documents with concise resume 
- Search uploaded content through chat-window query
- Will generate search results/response to query and respond with top attached references (if any)
- Fast search/query response, even through bulk upload of large files and high page count
- Responsive UI
  
![firefox_Oz8WByliLZ](https://github.com/user-attachments/assets/d4da0376-421a-402e-b4e7-7e69284d4e0f)

# Built with
- ASP.NET WebAPI .NET 8 + Swagger
- Blazor .NET 8
- SQLite
- OpenAI embedding & chunking
- FAISS
- Vector search
- GPT-completion (3.5 turbo for demo)

# Run demo
- Requires active OpenAI API key, insert at pdfEngineAPI/appsettings.Development.json 
- Run ASP.NET migrations for EF/SQLite file persistance
- Demo configuration, will purposely exit and throw uncaught exception.
- Contact me
