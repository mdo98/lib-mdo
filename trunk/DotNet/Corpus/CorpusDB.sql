/* ============================================================ */
/* Schema: RefCorpus */

CREATE SCHEMA [RefCorpus];
GO

/* ============================================================ */
/* Table: Classes */

CREATE TABLE [RefCorpus].[Classes]
  ([Id] BIGINT IDENTITY(1,1) NOT NULL
  ,[Name] VARCHAR(108) NOT NULL
  );
GO

ALTER TABLE [RefCorpus].[Classes]
  ADD CONSTRAINT [PK__RefCorpus_Classes]
  PRIMARY KEY CLUSTERED
  ([Id] ASC
  );
GO

ALTER TABLE [RefCorpus].[Classes]
  ADD CONSTRAINT [UQ__RefCorpus_Classes__Name]
  UNIQUE NONCLUSTERED
  ([Name] ASC
  );
GO

/* ============================================================ */
/* Table: ClassVariants */

CREATE TABLE [RefCorpus].[ClassVariants]
  ([ClassId] BIGINT NOT NULL
  ,[VariantId] BIGINT IDENTITY(1,1) NOT NULL
  ,[Name] VARCHAR(100) NOT NULL
  ,[Ref] VARCHAR(128) NOT NULL
  ,[DfltColIndx] SMALLINT NOT NULL
  ,[Desc] NVARCHAR(MAX)
  ,[CreatedTime] DATETIME NOT NULL DEFAULT(GETUTCDATE())
  ,[LastModifiedTime] DATETIME NOT NULL DEFAULT(GETUTCDATE())
  );
GO

ALTER TABLE [RefCorpus].[ClassVariants]
  ADD CONSTRAINT [PK__RefCorpus_ClassVariants]
  PRIMARY KEY CLUSTERED
  ([ClassId] ASC
  ,[VariantId] ASC
  );
GO

ALTER TABLE [RefCorpus].[ClassVariants]
  ADD CONSTRAINT [UQ__RefCorpus_ClassVariants__Name]
  UNIQUE NONCLUSTERED
  ([ClassId] ASC
  ,[Name] ASC
  );
GO

ALTER TABLE [RefCorpus].[ClassVariants]
  WITH CHECK ADD CONSTRAINT [FK__RefCorpus_ClassVariants__RefCorpus_Classes]
  FOREIGN KEY
  ([ClassId]
  ) REFERENCES [RefCorpus].[Classes]
  ([Id]
  )
  ON DELETE CASCADE
  ON UPDATE CASCADE;
GO

ALTER TABLE [RefCorpus].[ClassVariants]
  WITH CHECK ADD CONSTRAINT [CK__RefCorpus_ClassVariants__DfltColIndx]
  CHECK([DfltColIndx] >= 0);
GO

/* ============================================================ */
/* Function: CountClassVariants */

CREATE FUNCTION [RefCorpus].[CountClassVariants]
(@ClassName VARCHAR(MAX)
) RETURNS INT
AS
BEGIN
DECLARE @Count INT;
SELECT @Count = COUNT(*) FROM [ClassVariants]
  LEFT JOIN [Classes] ON [ClassVariants].[ClassId] = [Classes].[Id]
 WHERE [Classes].[Name] = @ClassName;
RETURN @Count;
END
GO

/* ============================================================ */
/* Function: VariantRef */

CREATE FUNCTION [RefCorpus].[VariantRef]
(@ClassName VARCHAR(MAX)
,@VariantName VARCHAR(MAX)
) RETURNS VARCHAR(MAX)
AS
BEGIN
DECLARE @Ref VARCHAR(MAX);
SELECT @Ref = [Ref] FROM [ClassVariants]
  LEFT JOIN [Classes] ON [ClassVariants].[ClassId] = [Classes].[Id]
 WHERE [Classes].[Name] = @ClassName
   AND [ClassVariants].[Name] = @VariantName;
RETURN @Ref;
END
GO

/* ============================================================ */
/* Procedure: ListClasses */

CREATE PROCEDURE [RefCorpus].[ListClasses]
AS
BEGIN
SELECT [Name] FROM [Classes]
 ORDER BY [Name];
END
GO

/* ============================================================ */
/* Procedure: ListClassVariants */

CREATE PROCEDURE [RefCorpus].[ListClassVariants]
(@ClassName VARCHAR(MAX)
)
AS
BEGIN
SELECT [ClassVariants].[Name]
      ,[Ref]
	  ,[DfltColIndx]
      ,[Desc]
      ,[CreatedTime]
      ,[LastModifiedTime]
  FROM [ClassVariants]
  LEFT JOIN [Classes] ON [ClassVariants].[ClassId] = [Classes].[Id]
 WHERE [Classes].[Name] = @ClassName
 ORDER BY [VariantId];
END
GO

/* ============================================================ */
/* Procedure: GetClassVariant */

CREATE PROCEDURE [RefCorpus].[GetClassVariant]
(@ClassName VARCHAR(MAX)
,@VariantName VARCHAR(MAX)
)
AS
BEGIN
SELECT [Ref]
	  ,[DfltColIndx]
      ,[Desc]
      ,[CreatedTime]
      ,[LastModifiedTime]
  FROM [ClassVariants]
  LEFT JOIN [Classes] ON [ClassVariants].[ClassId] = [Classes].[Id]
 WHERE [Classes].[Name] = @ClassName
   AND [ClassVariants].[Name] = @VariantName;
END
GO

/* ============================================================ */
/* Procedure: AddClassVariant */

CREATE PROCEDURE [RefCorpus].[AddClassVariant]
(@ClassName VARCHAR(MAX)
,@VariantName VARCHAR(MAX)
,@DfltColIndx SMALLINT
,@Desc NVARCHAR(MAX) = NULL
)
AS
BEGIN
DECLARE @ClassId BIGINT;
SELECT @ClassId = [Id] FROM [Classes]
 WHERE [Name] = @ClassName;

IF @ClassId IS NULL
BEGIN
  INSERT INTO [Classes]
    ([Name])
    VALUES
    (@ClassName);

  SET @ClassId = SCOPE_IDENTITY();
END

INSERT INTO [ClassVariants]
  ([ClassId], [Name], [Ref], [DfltColIndx], [Desc])
  VALUES
  (@ClassId, @VariantName, '', @DfltColIndx, @Desc);

DECLARE @VariantId BIGINT;
SET @VariantId = SCOPE_IDENTITY();

DECLARE @Ref VARCHAR(MAX);
SET @Ref = @ClassName + '_' + CONVERT(VARCHAR(MAX), @VariantId);

UPDATE [ClassVariants]
   SET [Ref] = @Ref
 WHERE [ClassId] = @ClassId
   AND [VariantId] = @VariantId;

SELECT @Ref;
END
GO

/* ============================================================ */
/* Procedure: EditClassVariant */

CREATE PROCEDURE [RefCorpus].[EditClassVariant]
(@ClassName VARCHAR(MAX)
,@VariantName VARCHAR(MAX)
,@DfltColIndx SMALLINT
,@Desc NVARCHAR(MAX) = NULL
)
AS
BEGIN
DECLARE @ClassId BIGINT;
SELECT @ClassId = [Id] FROM [Classes]
 WHERE [Name] = @ClassName;

IF @ClassId IS NOT NULL
BEGIN
  UPDATE [ClassVariants]
     SET [DfltColIndx] = @DfltColIndx
        ,[Desc] = COALESCE(@Desc, [Desc])
		,[LastModifiedTime] = GETUTCDATE()
   WHERE [ClassId] = @ClassId
     AND [Name] = @VariantName;
END
END
GO

/* ============================================================ */
/* Procedure: RemoveClassVariant */

CREATE PROCEDURE [RefCorpus].[RemoveClassVariant]
(@ClassName VARCHAR(MAX)
,@VariantName VARCHAR(MAX)
)
AS
BEGIN
DECLARE @ClassId BIGINT;
SELECT @ClassId = [Id] FROM [Classes]
 WHERE [Name] = @ClassName;

DELETE FROM [ClassVariants]
 WHERE [ClassId] = @ClassId
   AND [Name] = @VariantName;
END
GO

/* ============================================================ */
/* Procedure: CountClassItems */

CREATE PROCEDURE [RefCorpus].[CountClassItems]
(@ClassName VARCHAR(MAX)
,@VariantName VARCHAR(MAX)
)
AS
BEGIN

DECLARE @CMD VARCHAR(MAX);
SET @CMD = 'SELECT COUNT(*) FROM [RefCorpus].[' + [RefCorpus].[VariantRef](@ClassName, @VariantName) + '];';
EXEC(@CMD);

END
GO

/* ============================================================ */
/* Procedure: ListClassItems */

CREATE PROCEDURE [RefCorpus].[ListClassItems]
(@ClassName VARCHAR(MAX)
,@VariantName VARCHAR(MAX)
)
AS
BEGIN

DECLARE @CMD VARCHAR(MAX);
SET @CMD = 'SELECT * FROM [RefCorpus].[' + [RefCorpus].[VariantRef](@ClassName, @VariantName) + '];';
EXEC(@CMD);

END
GO

/* ============================================================ */
/* Procedure: PeekClassItems */

CREATE PROCEDURE [RefCorpus].[PeekClassItems]
(@ClassName VARCHAR(MAX)
,@VariantName VARCHAR(MAX)
,@NumItems INT
)
AS
BEGIN

DECLARE @CMD VARCHAR(MAX);
SET @CMD = 'SELECT TOP(' + CONVERT(VARCHAR(MAX), @NumItems) + ') * FROM [RefCorpus].[' + [RefCorpus].[VariantRef](@ClassName, @VariantName) + '];';
EXEC(@CMD);

END
GO

/* ============================================================ */
/* Procedure: GetClassItem */

CREATE PROCEDURE [RefCorpus].[GetClassItem]
(@ClassName VARCHAR(MAX)
,@VariantName VARCHAR(MAX)
,@ItemId INT
)
AS
BEGIN

DECLARE @CMD VARCHAR(MAX);
SET @CMD = 'SELECT * FROM [RefCorpus].[' + [RefCorpus].[VariantRef](@ClassName, @VariantName) + '] WHERE [Id] = ' + CONVERT(VARCHAR(MAX), @ItemId) + ';';
EXEC(@CMD);

END
GO
