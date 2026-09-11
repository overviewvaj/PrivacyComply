from fastapi import FastAPI, File, UploadFile

from app.file_inspector import inspect_uploaded_file


app = FastAPI(
    title="PrivacyComply Edge Agent",
    version="0.1.0",
)


@app.get("/health")
async def health() -> dict[str, str]:
    return {
        "status": "healthy",
        "service": "PrivacyComply.EdgeAgent",
    }


@app.post("/analysis/files/inspect")
async def inspect_file(
    file: UploadFile = File(...),
) -> dict:
    return await inspect_uploaded_file(
        file,
    )