/* =============================================================================
   PrivacyComply - Migration 003: Finding Table & Security Policy
   Purpose: Persist safe findings centrally with categories and statuses.
   ============================================================================= */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('workflow') AND name = 'Finding')
BEGIN
    CREATE TABLE workflow.Finding
    (
        FindingId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Finding_FindingId DEFAULT NEWID(),
        OrganisationId UNIQUEIDENTIFIER NOT NULL,
        AnalysisRunId UNIQUEIDENTIFIER NOT NULL,
        DiscoveredFieldId UNIQUEIDENTIFIER NULL,
        FindingCode NVARCHAR(100) NOT NULL,
        FindingCategory NVARCHAR(50) NOT NULL,
        Severity NVARCHAR(50) NOT NULL,
        FieldName NVARCHAR(255) NULL,
        RuleReference NVARCHAR(100) NULL,
        FindingStatus NVARCHAR(50) NOT NULL CONSTRAINT DF_Finding_FindingStatus DEFAULT N'OPEN',
        DetectedDateTime DATETIME2(3) NOT NULL CONSTRAINT DF_Finding_DetectedDateTime DEFAULT SYSUTCDATETIME(),
        ResolvedDateTime DATETIME2(3) NULL,
        Message NVARCHAR(1000) NULL,
        SafeMetadataJson NVARCHAR(MAX) NULL,
        CreatedDateTime DATETIME2(3) NOT NULL CONSTRAINT DF_Finding_CreatedDateTime DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(100) NOT NULL,
        UpdatedDateTime DATETIME2(3) NULL,
        UpdatedBy NVARCHAR(100) NULL,

        CONSTRAINT PK_Finding PRIMARY KEY CLUSTERED (FindingId),
        CONSTRAINT FK_Finding_AnalysisRun FOREIGN KEY (AnalysisRunId)
            REFERENCES workflow.AnalysisRun (AnalysisRunId) ON DELETE CASCADE,
        CONSTRAINT FK_Finding_DiscoveredField FOREIGN KEY (DiscoveredFieldId)
            REFERENCES workflow.DiscoveredField (DiscoveredFieldId),
        CONSTRAINT CK_Finding_Category CHECK (FindingCategory IN ('DATA_QUALITY', 'CLASSIFICATION', 'PRIVACY', 'REGULATORY')),
        CONSTRAINT CK_Finding_Severity CHECK (Severity IN ('INFO', 'WARNING', 'ERROR', 'CRITICAL')),
        CONSTRAINT CK_Finding_Status CHECK (FindingStatus IN ('OPEN', 'ACKNOWLEDGED', 'RESOLVED', 'DISMISSED'))
    );

    CREATE NONCLUSTERED INDEX IX_Finding_Run_Org
    ON workflow.Finding (AnalysisRunId, OrganisationId)
    INCLUDE (FindingCode, FindingCategory, Severity, FindingStatus, FieldName);

    CREATE NONCLUSTERED INDEX IX_Finding_Org_Status
    ON workflow.Finding (OrganisationId, FindingStatus, Severity)
    INCLUDE (AnalysisRunId, FindingCode, FieldName);
END
GO

-- Apply Row-Level Security (RLS)
IF NOT EXISTS (SELECT * FROM sys.security_policies WHERE name = 'FindingTenantSecurityPolicy')
BEGIN
    CREATE SECURITY POLICY security.FindingTenantSecurityPolicy
    ADD FILTER PREDICATE security.fn_TenantAccessPredicate(OrganisationId) ON workflow.Finding,
    ADD BLOCK PREDICATE security.fn_TenantAccessPredicate(OrganisationId) ON workflow.Finding
    WITH (STATE = ON);
END
GO
