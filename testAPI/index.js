const express = require("express");
const multer = require("multer");
const fs = require("fs");
const app = express();
const uploadDir = "uploads";
const upload = multer({ dest: uploadDir });

app.get("/download", (req, res) => {
  const fileId = req.query.fileId; 

  const filePath = `./${uploadDir}/${fileId}`;

  if (!fs.existsSync(filePath)) {
    return res.status(404).send("File not found");
  }

  res.contentType("application/octet-stream");
  res.setHeader("Content-Disposition", `attachment; filename=${fileId}`);

  fs.createReadStream(filePath).pipe(res);
});

app.post("/upload", upload.single("file"), (req, res) => {
  const uploadedFile = req.file;

  if (!uploadedFile) {
    return res.status(400).send("No file uploaded");
  }

  const newFilename = `${Date.now()}-${uploadedFile.originalname}`;
  fs.renameSync(uploadedFile.path, `${uploadDir}/${newFilename}`);

  res.json({
    filename: newFilename,
    size: uploadedFile.size,
    mimeType: uploadedFile.mimetype,
  });
});

app.use((err, req, res, next) => {
  console.error(err.stack);
  res.status(500).send("Internal Server Error");
});

const port = process.env.PORT || 3000;
app.listen(port, () => console.log(`Server listening on port ${port}`));
