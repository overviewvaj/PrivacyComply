"""Safe Evidence Generator for PrivacyComply Edge Agent.

Generates tamper-evident, non-invertible audit evidence for local file
inspections and column classifications without storing or hashing raw
personal values.
"""

import hashlib
import json
from typing import Any

AGENT_VERSION: str = "1.0.0"
RULE_VERSION: str = "dpdp-v1.0.0"
HASH_ALGORITHM: str = "SHA-256"


def compute_safe_hash(parts: list[str | int | float | bool | None]) -> str:
    """Compute a deterministic SHA-256 hash over normalized metadata parts.
    
    Invariant: No raw personal data values are ever passed to this function.
    """
    normalized = "|".join(str(part) if part is not None else "" for part in parts)
    return hashlib.sha256(normalized.encode("utf-8")).hexdigest()


def generate_field_evidence(
    file_name: str,
    sheet_name: str | None,
    profile: dict[str, Any],
) -> dict[str, Any]:
    """Generate a safe evidence record for a discovered column/field."""
    column_name = profile.get("columnName", "")
    classification_code = profile.get("classificationCode")
    classification_method = profile.get("classificationMethod", "NO_MATCH")
    inferred_type = profile.get("inferredDataType", "TEXT")
    privacy_category = profile.get("privacyCategory", "NOT_PERSONAL")
    is_personal_data = profile.get("isPersonalData", False)
    is_regulated_identifier = profile.get("isRegulatedIdentifier", False)
    match_percentage = profile.get("matchPercentage")
    ordinal_position = profile.get("ordinalPosition", 0)

    # Hash strictly over structural and classification metadata
    evidence_hash = compute_safe_hash([
        "FIELD",
        file_name,
        sheet_name or "",
        column_name,
        inferred_type,
        classification_code or "UNCLASSIFIED",
        classification_method,
        privacy_category,
        is_regulated_identifier,
        AGENT_VERSION,
        RULE_VERSION,
    ])

    metadata = {
        "columnName": column_name,
        "ordinalPosition": ordinal_position,
        "inferredDataType": inferred_type,
        "classificationStatus": profile.get("classificationStatus"),
        "classificationCode": classification_code,
        "classificationMethod": classification_method,
        "matchPercentage": match_percentage,
        "privacyCategory": privacy_category,
        "isPersonalData": is_personal_data,
        "isRegulatedIdentifier": is_regulated_identifier,
        "nonEmptyCount": profile.get("nonEmptyCount", 0),
        "emptyCount": profile.get("emptyCount", 0),
    }

    clean_ref = f"EVD-{file_name}-COL-{column_name}".replace(" ", "_")

    return {
        "evidenceReference": clean_ref,
        "evidenceType": "FIELD_CLASSIFICATION",
        "sourceReference": file_name,
        "fieldName": column_name,
        "classificationCode": classification_code,
        "ruleVersion": RULE_VERSION,
        "agentVersion": AGENT_VERSION,
        "evidenceHash": evidence_hash,
        "hashAlgorithm": HASH_ALGORITHM,
        "metadataJson": json.dumps(metadata, sort_keys=True),
    }


def generate_run_evidence(
    file_name: str,
    sheet_name: str | None,
    row_count: int,
    column_count: int,
    classification_summary: dict[str, Any],
) -> dict[str, Any]:
    """Generate a safe evidence record for the entire file inspection run."""
    coverage_pct = classification_summary.get("classificationCoveragePercentage", 0.0)
    personal_data_count = classification_summary.get("totalPersonalDataColumns", 0)
    classified_count = classification_summary.get("classifiedColumns", 0)
    unclassified_count = classification_summary.get("unclassifiedColumns", 0)

    # Hash strictly over aggregate run metrics and context
    evidence_hash = compute_safe_hash([
        "RUN",
        file_name,
        sheet_name or "",
        row_count,
        column_count,
        coverage_pct,
        personal_data_count,
        AGENT_VERSION,
        RULE_VERSION,
    ])

    metadata = {
        "fileName": file_name,
        "sheetName": sheet_name,
        "rowCount": row_count,
        "columnCount": column_count,
        "classifiedColumns": classified_count,
        "unclassifiedColumns": unclassified_count,
        "classificationCoveragePercentage": coverage_pct,
        "totalPersonalDataColumns": personal_data_count,
        "processingLocation": "LOCAL_EDGE_AGENT",
    }

    clean_ref = f"EVD-{file_name}-RUN".replace(" ", "_")

    return {
        "evidenceReference": clean_ref,
        "evidenceType": "ANALYSIS_RUN",
        "sourceReference": file_name,
        "fieldName": None,
        "classificationCode": None,
        "ruleVersion": RULE_VERSION,
        "agentVersion": AGENT_VERSION,
        "evidenceHash": evidence_hash,
        "hashAlgorithm": HASH_ALGORITHM,
        "metadataJson": json.dumps(metadata, sort_keys=True),
    }


def generate_inspection_evidence(
    file_name: str,
    sheet_name: str | None,
    row_count: int,
    column_count: int,
    column_profiles: list[dict[str, Any]],
    classification_summary: dict[str, Any],
) -> list[dict[str, Any]]:
    """Generate all safe audit evidence records for an inspection."""
    evidence_list: list[dict[str, Any]] = []

    # 1. Run-level evidence
    evidence_list.append(
        generate_run_evidence(
            file_name=file_name,
            sheet_name=sheet_name,
            row_count=row_count,
            column_count=column_count,
            classification_summary=classification_summary,
        )
    )

    # 2. Field-level evidence for each column
    for profile in column_profiles:
        evidence_list.append(
            generate_field_evidence(
                file_name=file_name,
                sheet_name=sheet_name,
                profile=profile,
            )
        )

    return evidence_list
