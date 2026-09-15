/* =============================================================================
   PrivacyComply - Migration 005: Fix Evidence Indexes
   Purpose: Replace overly restrictive UX_EvidenceRecord_Source unique index with
            non-unique lookup index so multiple runs and multiple fields per source
            can be recorded.
   ============================================================================= */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_EvidenceRecord_Source' AND object_id = OBJECT_ID('evidence.EvidenceRecord'))
BEGIN
    DROP INDEX UX_EvidenceRecord_Source ON evidence.EvidenceRecord;
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EvidenceRecord_Source' AND object_id = OBJECT_ID('evidence.EvidenceRecord'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_EvidenceRecord_Source
    ON evidence.EvidenceRecord (OrganisationId, EvidenceTypeCode, SourceTypeCode, SourceReference)
    WHERE [SourceReference] IS NOT NULL;
END
GO
