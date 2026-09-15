/* =============================================================================
   PrivacyComply - Migration 004: Extend EvidenceRecord & Link to AnalysisRun
   Purpose: Store tamper-evident, non-invertible audit evidence for runs and
            field classifications without storing underlying customer personal data.
   ============================================================================= */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

-- 1. Add AnalysisRun and Field linking columns to evidence.EvidenceRecord
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('evidence.EvidenceRecord') AND name = 'AnalysisRunId')
BEGIN
    ALTER TABLE evidence.EvidenceRecord
    ADD AnalysisRunId UNIQUEIDENTIFIER NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('evidence.EvidenceRecord') AND name = 'DiscoveredFieldId')
BEGIN
    ALTER TABLE evidence.EvidenceRecord
    ADD DiscoveredFieldId UNIQUEIDENTIFIER NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('evidence.EvidenceRecord') AND name = 'RuleVersion')
BEGIN
    ALTER TABLE evidence.EvidenceRecord
    ADD RuleVersion NVARCHAR(50) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('evidence.EvidenceRecord') AND name = 'AgentVersion')
BEGIN
    ALTER TABLE evidence.EvidenceRecord
    ADD AgentVersion NVARCHAR(50) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('evidence.EvidenceRecord') AND name = 'MetadataJson')
BEGIN
    ALTER TABLE evidence.EvidenceRecord
    ADD MetadataJson NVARCHAR(MAX) NULL;
END
GO

-- 2. Add Foreign Keys from evidence.EvidenceRecord to workflow tables
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_EvidenceRecord_AnalysisRun')
BEGIN
    ALTER TABLE evidence.EvidenceRecord
    ADD CONSTRAINT FK_EvidenceRecord_AnalysisRun
        FOREIGN KEY (AnalysisRunId) REFERENCES workflow.AnalysisRun (AnalysisRunId) ON DELETE CASCADE;
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_EvidenceRecord_DiscoveredField')
BEGIN
    ALTER TABLE evidence.EvidenceRecord
    ADD CONSTRAINT FK_EvidenceRecord_DiscoveredField
        FOREIGN KEY (DiscoveredFieldId) REFERENCES workflow.DiscoveredField (DiscoveredFieldId);
END
GO

-- 3. Add EvidenceRecordId to workflow.Finding
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('workflow.Finding') AND name = 'EvidenceRecordId')
BEGIN
    ALTER TABLE workflow.Finding
    ADD EvidenceRecordId UNIQUEIDENTIFIER NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Finding_EvidenceRecord')
BEGIN
    ALTER TABLE workflow.Finding
    ADD CONSTRAINT FK_Finding_EvidenceRecord
        FOREIGN KEY (EvidenceRecordId) REFERENCES evidence.EvidenceRecord (EvidenceRecordId);
END
GO

-- 4. Create Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EvidenceRecord_Run_Org' AND object_id = OBJECT_ID('evidence.EvidenceRecord'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_EvidenceRecord_Run_Org
    ON evidence.EvidenceRecord (AnalysisRunId, OrganisationId)
    INCLUDE (EvidenceReference, EvidenceTypeCode, EvidenceHash, CapturedDateTime);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EvidenceRecord_Field' AND object_id = OBJECT_ID('evidence.EvidenceRecord'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_EvidenceRecord_Field
    ON evidence.EvidenceRecord (DiscoveredFieldId)
    WHERE DiscoveredFieldId IS NOT NULL;
END
GO

-- 5. Create or Replace workflow.vw_RunEvidence View
CREATE OR ALTER VIEW workflow.vw_RunEvidence AS
SELECT 
    e.EvidenceRecordId AS EvidenceId,
    e.OrganisationId,
    e.AnalysisRunId,
    e.DiscoveredFieldId,
    e.EvidenceReference,
    e.EvidenceTypeCode AS EvidenceType,
    e.SourceTypeCode AS SourceType,
    e.SourceReference,
    e.RuleVersion,
    e.AgentVersion,
    e.EvidenceHash,
    e.HashAlgorithmCode,
    e.CapturedDateTime AS GeneratedDateTime,
    e.CapturedBy,
    e.IsVerified,
    e.VerificationStatusCode,
    e.MetadataJson,
    df.FieldName,
    df.SourceObjectName,
    df.ClassificationCode,
    df.PrivacyCategory,
    df.IsPersonalData,
    df.IsRegulatedIdentifier
FROM evidence.EvidenceRecord e
LEFT JOIN workflow.DiscoveredField df ON e.DiscoveredFieldId = df.DiscoveredFieldId;
GO
