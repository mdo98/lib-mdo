/* ============================================================ */
/* Schema: DataIO */

CREATE SCHEMA [DataIO];
GO

/* ============================================================ */
/* Table: Folders */

CREATE TABLE [DataIO].[Folders]
  ([Id] BIGINT IDENTITY(0,1) NOT NULL
  ,[Name] VARCHAR(128) NOT NULL
  );
GO

ALTER TABLE [DataIO].[Folders]
  ADD CONSTRAINT [PK__DataIO_Folders]
  PRIMARY KEY CLUSTERED
  ([Id] ASC
  );
GO

ALTER TABLE [DataIO].[Folders]
  ADD CONSTRAINT [UQ__DataIO_Folders__Name]
  UNIQUE NONCLUSTERED
  ([Name] ASC
  );
GO

/* ============================================================ */
/* Table: Files */

CREATE TABLE [DataIO].[Files]
  ([FolderId] BIGINT NOT NULL
  ,[FileId] BIGINT IDENTITY(0,1) NOT NULL
  ,[Name] VARCHAR(128) NOT NULL
  ,[DfltColIndx] SMALLINT NOT NULL
  ,[Desc] NVARCHAR(MAX)
  ,[CreatedTime] DATETIME NOT NULL DEFAULT(GETUTCDATE())
  ,[LastModifiedTime] DATETIME NOT NULL DEFAULT(GETUTCDATE())
  );
GO

ALTER TABLE [DataIO].[Files]
  ADD CONSTRAINT [PK__DataIO_Files]
  PRIMARY KEY CLUSTERED
  ([FolderId] ASC
  ,[FileId] ASC
  );
GO

ALTER TABLE [DataIO].[Files]
  ADD CONSTRAINT [UQ__DataIO_Files__Name]
  UNIQUE NONCLUSTERED
  ([FolderId] ASC
  ,[Name] ASC
  );
GO

ALTER TABLE [DataIO].[Files]
  WITH CHECK ADD CONSTRAINT [FK__DataIO_Files__DataIO_Folders]
  FOREIGN KEY
  ([FolderId]
  ) REFERENCES [DataIO].[Folders]
  ([Id]
  )
  ON DELETE CASCADE
  ON UPDATE CASCADE;
GO

ALTER TABLE [DataIO].[Files]
  WITH CHECK ADD CONSTRAINT [CK__DataIO_Files__DfltColIndx]
  CHECK([DfltColIndx] >= 0);
GO

/* ============================================================ */
/* Function: CountFiles */

CREATE FUNCTION [DataIO].[CountFiles]
(@FolderName VARCHAR(MAX)
) RETURNS INT
AS
BEGIN
DECLARE @Count INT;
SELECT @Count = COUNT(*) FROM [Files]
  LEFT JOIN [Folders] ON [Files].[FolderId] = [Folders].[Id]
 WHERE [Folders].[Name] = @FolderName;
RETURN @Count;
END
GO

/* ============================================================ */
/* Function: FileExists */

CREATE FUNCTION [DataIO].[FileExists]
(@FolderName VARCHAR(MAX)
,@FileName VARCHAR(MAX)
) RETURNS BIT
AS
BEGIN
DECLARE @Exists BIT;
SELECT @Exists = 1 FROM [Files]
  LEFT JOIN [Folders] ON [Files].[FolderId] = [Folders].[Id]
 WHERE [Folders].[Name] = @FolderName
   AND [Files].[Name] = @FileName;
SET @Exists = COALESCE(@Exists, 0);
RETURN @Exists;
END
GO

/* ============================================================ */
/* Procedure: ListFolders */

CREATE PROCEDURE [DataIO].[ListFolders]
AS
BEGIN
SELECT [Name] FROM [Folders]
 ORDER BY [Name];
END
GO

/* ============================================================ */
/* Procedure: ListFiles */

CREATE PROCEDURE [DataIO].[ListFiles]
(@FolderName VARCHAR(MAX)
)
AS
BEGIN
SELECT [Files].[Name]
	  ,[DfltColIndx]
      ,[Desc]
      ,[CreatedTime]
      ,[LastModifiedTime]
  FROM [Files]
  LEFT JOIN [Folders] ON [Files].[FolderId] = [Folders].[Id]
 WHERE [Folders].[Name] = @FolderName
 ORDER BY [FileId];
END
GO

/* ============================================================ */
/* Procedure: GetFile */

CREATE PROCEDURE [DataIO].[GetFile]
(@FolderName VARCHAR(MAX)
,@FileName VARCHAR(MAX)
)
AS
BEGIN
SELECT [Files].[Name]
	  ,[DfltColIndx]
      ,[Desc]
      ,[CreatedTime]
      ,[LastModifiedTime]
  FROM [Files]
  LEFT JOIN [Folders] ON [Files].[FolderId] = [Folders].[Id]
 WHERE [Folders].[Name] = @FolderName
   AND [Files].[Name] = @FileName;
END
GO

/* ============================================================ */
/* Procedure: AddFile */

CREATE PROCEDURE [DataIO].[AddFile]
(@FolderName VARCHAR(MAX)
,@FileName VARCHAR(MAX)
,@DfltColIndx SMALLINT
,@Desc NVARCHAR(MAX) = NULL
)
AS
BEGIN
DECLARE @FolderId BIGINT;
SELECT @FolderId = [Id] FROM [Folders]
 WHERE [Name] = @FolderName;

IF @FolderId IS NULL
BEGIN
  INSERT INTO [Folders]
    ([Name])
    VALUES
    (@FolderName);

  SET @FolderId = SCOPE_IDENTITY();
END

INSERT INTO [Files]
  ([FolderId], [Name], [DfltColIndx], [Desc])
  VALUES
  (@FolderId, @FileName, @DfltColIndx, @Desc);
END
GO

/* ============================================================ */
/* Procedure: EditFile */

CREATE PROCEDURE [DataIO].[EditFile]
(@FolderName VARCHAR(MAX)
,@FileName VARCHAR(MAX)
,@DfltColIndx SMALLINT
,@Desc NVARCHAR(MAX) = NULL
)
AS
BEGIN
DECLARE @FolderId BIGINT;
SELECT @FolderId = [Id] FROM [Folders]
 WHERE [Name] = @FolderName;

IF @FolderId IS NOT NULL
BEGIN
  UPDATE [Files]
     SET [DfltColIndx] = @DfltColIndx
        ,[Desc] = COALESCE(@Desc, [Desc])
		,[LastModifiedTime] = GETUTCDATE()
   WHERE [FolderId] = @FolderId
     AND [Name] = @FileName;
END
END
GO

/* ============================================================ */
/* Procedure: RemoveFile */

CREATE PROCEDURE [DataIO].[RemoveFile]
(@FolderName VARCHAR(MAX)
,@FileName VARCHAR(MAX)
)
AS
BEGIN
DECLARE @FolderId BIGINT;
SELECT @FolderId = [Id] FROM [Folders]
 WHERE [Name] = @FolderName;

IF @FolderId IS NOT NULL
BEGIN
  DELETE FROM [Files]
   WHERE [FolderId] = @FolderId
     AND [Name] = @FileName;
END
END
GO
