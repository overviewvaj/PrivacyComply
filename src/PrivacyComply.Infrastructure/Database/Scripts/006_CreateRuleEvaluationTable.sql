/* =============================================================================
   PrivacyComply - Migration 006: RuleEvaluation Table, View & Tenant Security
   Purpose: Persist DPDP regulatory rules evaluations with versioning & outcomes.
   ============================================================================= */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE schema_id = SCHEMA_ID('workflow') AND name = 'RuleEvaluation')
BEGIN
    CREATE TABLE workflow.RuleEvaluation
    (
        RuleEvaluationId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_RuleEvaluation_RuleEvaluationId DEFAULT NEWID(),
        OrganisationId UNIQUEIDENTIFIER NOT NULL,
        AnalysisRunId UNIQUEIDENTIFIER NOT NULL,
        FrameworkCode NVARCHAR(50) NOT NULL CONSTRAINT DF_RuleEvaluation_FrameworkCode DEFAULT N'DPDP',
        FrameworkVersion NVARCHAR(50) NOT NULL CONSTRAINT DF_RuleEvaluation_FrameworkVersion DEFAULT N'2023-v1.0',
        RuleCode NVARCHAR(100) NOT NULL,
        RuleVersion NVARCHAR(50) NOT NULL CONSTRAINT DF_RuleEvaluation_RuleVersion DEFAULT N'1.0.0',
        RuleName NVARCHAR(250) NOT NULL,
        RegulatoryReference NVARCHAR(500) NULL,
        EvaluationOutcome NVARCHAR(50) NOT NULL,
        Severity NVARCHAR(30) NOT NULL CONSTRAINT DF_RuleEvaluation_Severity DEFAULT N'INFO',
        SummaryMessage NVARCHAR(2000) NOT NULL,
        EvaluatedFieldsCount INT NOT NULL CONSTRAINT DF_RuleEvaluation_EvaluatedFieldsCount DEFAULT 0,
        FlaggedFieldsCount INT NOT NULL CONSTRAINT DF_RuleEvaluation_FlaggedFieldsCount DEFAULT 0,
        FlaggedFieldNamesJson NVARCHAR(MAX) NULL,
        EvaluatedDateTime DATETIME2(3) NOT NULL CONSTRAINT DF_RuleEvaluation_EvaluatedDateTime DEFAULT SYSUTCDATETIME(),
        CreatedDateTime DATETIME2(3) NOT NULL CONSTRAINT DF_RuleEvaluation_CreatedDateTime DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(100) NOT NULL,
        UpdatedDateTime DATETIME2(3) NULL,
        UpdatedBy NVARCHAR(100) NULL,

        CONSTRAINT PK_RuleEvaluation PRIMARY KEY CLUSTERED (RuleEvaluationId),
        CONSTRAINT FK_RuleEvaluation_AnalysisRun FOREIGN KEY (AnalysisRunId)
            REFERENCES workflow.AnalysisRun (AnalysisRunId) ON DELETE CASCADE,
        CONSTRAINT CK_RuleEvaluation_Outcome CHECK (EvaluationOutcome IN ('PASS', 'FAIL', 'REVIEW_REQUIRED', 'NOT_APPLICABLE')),
        CONSTRAINT CK_RuleEvaluation_Severity CHECK (Severity IN ('CRITICAL', 'HIGH', 'MEDIUM', 'LOW', 'INFO'))
    );

    CREATE NONCLUSTERED INDEX IX_RuleEvaluation_Run_Org
    ON workflow.RuleEvaluation (AnalysisRunId, OrganisationId)
    INCLUDE (RuleCode, RuleVersion, EvaluationOutcome, Severity);

    CREATE NONCLUSTERED INDEX IX_RuleEvaluation_Org_Outcome
    ON workflow.RuleEvaluation (OrganisationId, EvaluationOutcome, Severity)
    INCLUDE (AnalysisRunId, RuleCode, FrameworkCode);
END
GO

-- Create View workflow.vw_RunRuleEvaluations
IF OBJECT_ID('workflow.vw_RunRuleEvaluations', 'V') IS NOT NULL
    DROP VIEW workflow.vw_RunRuleEvaluations;
GO

CREATE VIEW workflow.vw_RunRuleEvaluations
AS
SELECT
    re.RuleEvaluationId,
    re.OrganisationId,
    re.AnalysisRunId,
    ar.AnalysisRunCode,
    re.FrameworkCode,
    re.FrameworkVersion,
    re.RuleCode,
    re.RuleVersion,
    re.RuleName,
    re.RegulatoryReference,
    re.EvaluationOutcome,
    re.Severity,
    re.SummaryMessage,
    re.EvaluatedFieldsCount,
    re.FlaggedFieldsCount,
    re.FlaggedFieldNamesJson,
    re.EvaluatedDateTime,
    re.CreatedDateTime,
    re.CreatedBy
FROM workflow.RuleEvaluation re
INNER JOIN workflow.AnalysisRun ar
    ON re.AnalysisRunId = ar.AnalysisRunId
    AND re.OrganisationId = ar.OrganisationId;
GO

-- Apply Row-Level Security (RLS)
IF NOT EXISTS (SELECT * FROM sys.security_policies WHERE name = 'RuleEvaluationTenantSecurityPolicy')
BEGIN
    CREATE SECURITY POLICY security.RuleEvaluationTenantSecurityPolicy
    ADD FILTER PREDICATE security.fn_TenantAccessPredicate(OrganisationId) ON workflow.RuleEvaluation,
    ADD BLOCK PREDICATE security.fn_TenantAccessPredicate(OrganisationId) ON workflow.RuleEvaluation
    WITH (STATE = ON);
END
GO
