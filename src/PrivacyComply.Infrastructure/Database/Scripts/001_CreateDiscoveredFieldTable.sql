/* =============================================================================
   PrivacyComply - Migration 001: DiscoveredField Table & Security Policy
   Purpose: Persist discovered field metadata centrally without raw customer data.
   ============================================================================= */

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'workflow')
BEGIN
    EXEC('CREATE SCHEMA workflow;');
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('workflow') AND name = 'DiscoveredField')
BEGIN
    CREATE TABLE workflow.DiscoveredField
    (
        DiscoveredFieldId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_DiscoveredField_DiscoveredFieldId DEFAULT NEWID(),
        OrganisationId UNIQUEIDENTIFIER NOT NULL,
        AnalysisRunId UNIQUEIDENTIFIER NOT NULL,
        SourceObjectName NVARCHAR(255) NULL,
        FieldName NVARCHAR(255) NOT NULL,
        OrdinalPosition INT NOT NULL,
        InferredDataType NVARCHAR(50) NOT NULL,
        ClassificationStatus NVARCHAR(50) NOT NULL,
        ClassificationCode NVARCHAR(100) NULL,
        ClassificationMethod NVARCHAR(100) NOT NULL,
        MatchPercentage DECIMAL(5,2) NULL,
        PrivacyCategory NVARCHAR(50) NOT NULL,
        IsPersonalData BIT NOT NULL CONSTRAINT DF_DiscoveredField_IsPersonalData DEFAULT 0,
        IsRegulatedIdentifier BIT NOT NULL CONSTRAINT DF_DiscoveredField_IsRegulatedIdentifier DEFAULT 0,
        NonEmptyCount BIGINT NOT NULL CONSTRAINT DF_DiscoveredField_NonEmptyCount DEFAULT 0,
        EmptyCount BIGINT NOT NULL CONSTRAINT DF_DiscoveredField_EmptyCount DEFAULT 0,
        FindingCount INT NOT NULL CONSTRAINT DF_DiscoveredField_FindingCount DEFAULT 0,
        CreatedDateTime DATETIME2(3) NOT NULL CONSTRAINT DF_DiscoveredField_CreatedDateTime DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(100) NOT NULL,

        CONSTRAINT PK_DiscoveredField PRIMARY KEY CLUSTERED (DiscoveredFieldId),
        CONSTRAINT FK_DiscoveredField_AnalysisRun FOREIGN KEY (AnalysisRunId)
            REFERENCES workflow.AnalysisRun (AnalysisRunId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_DiscoveredField_Run_Org
    ON workflow.DiscoveredField (AnalysisRunId, OrganisationId)
    INCLUDE (FieldName, ClassificationCode, PrivacyCategory, IsPersonalData);

    CREATE NONCLUSTERED INDEX IX_DiscoveredField_Org_Privacy
    ON workflow.DiscoveredField (OrganisationId, PrivacyCategory, IsPersonalData)
    INCLUDE (AnalysisRunId, FieldName, ClassificationCode);
END
GO

-- Apply Row-Level Security (RLS)
IF NOT EXISTS (SELECT * FROM sys.security_policies WHERE name = 'DiscoveredFieldTenantSecurityPolicy')
BEGIN
    CREATE SECURITY POLICY security.DiscoveredFieldTenantSecurityPolicy
    ADD FILTER PREDICATE security.fn_TenantAccessPredicate(OrganisationId) ON workflow.DiscoveredField,
    ADD BLOCK PREDICATE security.fn_TenantAccessPredicate(OrganisationId) ON workflow.DiscoveredField
    WITH (STATE = ON);
END
GO
