
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

-- 1. Ensure statutory and product-defined DPDP categories exist in classification.Category
IF NOT EXISTS (SELECT 1 FROM classification.Category WHERE CategoryCode = 'PERSONAL_DATA')
BEGIN
    INSERT INTO classification.Category
    (
        CategoryId, CategoryCode, CategoryName, Description,
        SensitivityLevelCode, IsPersonalData, IsSensitiveData,
        StatusCode, CreatedDateTime, CreatedBy
    )
    VALUES
    (
        NEWID(), 'PERSONAL_DATA', 'Personal Data',
        'Statutory personal data under DPDP Act 2023 identifying an individual directly or indirectly.',
        'MEDIUM', 1, 0, 'ACTIVE', SYSUTCDATETIME(), 'system-init'
    );
    PRINT 'Inserted category: PERSONAL_DATA';
END

IF NOT EXISTS (SELECT 1 FROM classification.Category WHERE CategoryCode = 'CONTEXT_DEPENDENT')
BEGIN
    INSERT INTO classification.Category
    (
        CategoryId, CategoryCode, CategoryName, Description,
        SensitivityLevelCode, IsPersonalData, IsSensitiveData,
        StatusCode, CreatedDateTime, CreatedBy
    )
    VALUES
    (
        NEWID(), 'CONTEXT_DEPENDENT', 'Context Dependent Data',
        'Data that may constitute personal data only when linked with specific external context or identifiers.',
        'LOW', 0, 0, 'ACTIVE', SYSUTCDATETIME(), 'system-init'
    );
    PRINT 'Inserted category: CONTEXT_DEPENDENT';
END

IF NOT EXISTS (SELECT 1 FROM classification.Category WHERE CategoryCode = 'REGULATED_IDENTIFIER')
BEGIN
    INSERT INTO classification.Category
    (
        CategoryId, CategoryCode, CategoryName, Description,
        SensitivityLevelCode, IsPersonalData, IsSensitiveData,
        StatusCode, CreatedDateTime, CreatedBy
    )
    VALUES
    (
        NEWID(), 'REGULATED_IDENTIFIER', 'Regulated Identifier',
        'Statutory personal identifiers with special regulatory obligations (e.g. Aadhaar Act, Income Tax PAN).',
        'HIGH', 1, 0, 'ACTIVE', SYSUTCDATETIME(), 'system-init'
    );
    PRINT 'Inserted category: REGULATED_IDENTIFIER';
END

-- 2. Add Check Constraint on workflow.DiscoveredField.PrivacyCategory
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_DiscoveredField_PrivacyCategory')
BEGIN
    ALTER TABLE workflow.DiscoveredField
    ADD CONSTRAINT CK_DiscoveredField_PrivacyCategory
    CHECK (PrivacyCategory IN ('PERSONAL_DATA', 'CONTEXT_DEPENDENT', 'NOT_PERSONAL'));
    PRINT 'Added check constraint CK_DiscoveredField_PrivacyCategory';
END

-- 3. Add view workflow.vw_RunFieldsSummary
IF NOT EXISTS (SELECT 1 FROM sys.views WHERE schema_id = SCHEMA_ID('workflow') AND name = 'vw_RunFieldsSummary')
BEGIN
    EXEC('
    CREATE VIEW workflow.vw_RunFieldsSummary
    AS
    SELECT
        df.DiscoveredFieldId,
        df.OrganisationId,
        df.AnalysisRunId,
        ar.AnalysisRunCode,
        ar.RunStatusCode,
        df.SourceObjectName,
        df.FieldName,
        df.OrdinalPosition,
        df.InferredDataType,
        df.ClassificationStatus,
        df.ClassificationCode,
        df.ClassificationMethod,
        df.MatchPercentage,
        df.PrivacyCategory,
        df.IsPersonalData,
        df.IsRegulatedIdentifier,
        CASE
            WHEN df.ClassificationStatus = ''UNCLASSIFIED'' THEN ''UNCLASSIFIED_REVIEW''
            WHEN df.PrivacyCategory = ''CONTEXT_DEPENDENT'' THEN ''CONTEXT_REVIEW''
            WHEN df.MatchPercentage < 90.0 THEN ''CONFIDENCE_REVIEW''
            ELSE ''STANDARD''
        END AS ReviewPriorityCode,
        df.NonEmptyCount,
        df.EmptyCount,
        df.FindingCount,
        df.CreatedDateTime
    FROM workflow.DiscoveredField df
    INNER JOIN workflow.AnalysisRun ar ON df.AnalysisRunId = ar.AnalysisRunId;
    ');
    PRINT 'Created view workflow.vw_RunFieldsSummary';
END

PRINT '--- All database amendments completed successfully ---';
