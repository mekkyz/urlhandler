const express = require("express");
const multer = require("multer");
const fs = require("fs");
const crypto = require("crypto");
const app = express();

const uploadDir = "uploads";

const authTokens = [];

// 5-digit random number for authtoken
const generateRandomNumber = () => {
  const firstDigit = Math.floor(Math.random() * 9) + 1;
  const randomNumber = firstDigit * 10000 + Math.floor(Math.random() * 90000);
  return randomNumber;
};

// renew the authtoken
const renewAuthToken = () => {
  const newAuthToken = generateRandomNumber();
  authTokens.push(newAuthToken);
  console.log(`Authtoken renewed: ${newAuthToken}`);
};

// generate new token on server start
const authToken = generateRandomNumber();
authTokens.push(authToken);

// validating authtoken
const validateAuthToken = (req, res, next) => {
  const providedToken = req.query.authtoken;

  if (!providedToken || !authTokens.includes(parseInt(providedToken))) {
    return res.status(401).send("Unauthorized");
  }

  next();
};

// download route
app.get("/download", validateAuthToken, (req, res) => {
  const fileId = req.query.fileId;
  const filePath = `./${uploadDir}/${fileId}`;

  if (!fs.existsSync(filePath)) {
    return res.status(404).send("File not found");
  }

  const fileSize = fs.statSync(filePath).size;

  res.contentType("application/octet-stream");
  res.setHeader("Content-Disposition", `attachment; filename=${fileId}`);
  res.setHeader("Content-Length", fileSize);

  fs.createReadStream(filePath).pipe(res);

  // token renewal after 15 minutes
  setTimeout(renewAuthToken, 900000);
});

// upload route
app.post(
  "/upload",
  validateAuthToken,
  multer({ dest: uploadDir }).single("file"),
  (req, res) => {
    const uploadedFile = req.file;
    res.contentType("application/octet-stream");
    res.setHeader(
      "Content-Disposition",
      `attachment; filename=${uploadedFile.filename}`
    );
    res.setHeader("Content-Length", uploadedFile.size);
    if (!uploadedFile) {
      return res.status(400).send("No file uploaded");
    }

    const newFilename = uploadedFile.originalname;
    fs.renameSync(uploadedFile.path, `${uploadDir}/${newFilename}`);

    res.json({
      filename: newFilename,
      size: uploadedFile.size,
      mimeType: uploadedFile.mimetype,
    });
  }
);

app.get("/get_token", (req, res) => {
  return res.status(200).json(authTokens[authTokens.length - 1]);
}),
  // renew endpoint
  app.post("/renew", (req, res) => {
    renewAuthToken();
    res.json({ authtoken: authTokens[authTokens.length - 1] });
  });

app.use((err, req, res, next) => {
  console.error(err.stack);
  res.status(500).send("Internal server error");
});

const port = process.env.PORT || 3000;
app.listen(port, () =>
  console.log(
    `Server listening on port ${port}\nInitial Authtoken: ${authTokens[0]}`
  )
);
