# PDGP / TradeLicence — WaterConnection module: documents stored in the DB

Uploaded documents are stored as bytes directly in the database
(`Application_Documents.File_Path`, `varbinary(max)`) instead of on the web
server's local disk, matching the schema exactly as you defined it — **no
extra columns**.

## Correction from the previous version

The first pass of this change added `File_Name` and `Content_Type` columns
to store the original filename and MIME type. That doesn't match your
schema, so it's been removed. Since there's no column to read a MIME type
from, the `Document` action now detects the content type by reading the
file's byte signature ("magic numbers") — it recognizes PDF, JPEG, PNG,
GIF and BMP, which covers the document/photo types this form accepts.
Anything else falls back to a generic download (`application/octet-stream`).
The downloaded filename is generated as `{DocumentPurpose}-{DocumentId}.ext`
(e.g. `NameAddress-14.pdf`) since the original filename isn't stored.

## Before you run it

Run `Database/VerifyApplicationDocumentsSchema.sql` against `NewEODB` to
confirm `Application_Documents.File_Path` is `varbinary(max)` on this
server. That's the only schema requirement — nothing else needs to change.

## Files touched (this correction)

- `TradeLicence/Models/ApplicationDocument.cs` — removed `FileName` /
  `ContentType` properties; only `FileContent` (mapped to `File_Path`)
  remains
- `TradeLicence/Data/WaterApplicationDbContext.cs` — removed the mappings
  for the two dropped columns
- `TradeLicence/Controllers/WaterConnectionController.cs` — `AddDocument`
  no longer sets a filename/content type; `Document(int id)` now sniffs the
  content type from the file's bytes via a new `DetectFileType` helper
- `Database/AlterApplicationDocuments_BinaryStorage.sql` — removed (no
  longer needed)
- `Database/VerifyApplicationDocumentsSchema.sql` — new, replaces it
