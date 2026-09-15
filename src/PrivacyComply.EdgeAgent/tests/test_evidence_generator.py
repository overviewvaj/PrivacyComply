import json
from app.evidence_generator import (
    AGENT_VERSION,
    HASH_ALGORITHM,
    RULE_VERSION,
    compute_safe_hash,
    generate_field_evidence,
    generate_inspection_evidence,
    generate_run_evidence,
)


def test_compute_safe_hash_deterministic():
    parts = ["FIELD", "customers.xlsx", "Sheet1", "AadhaarNumber", "TEXT", "INDIA_AADHAAR", "HEADER_RULE", "PERSONAL_DATA", True, "1.0.0", "dpdp-v1.0.0"]
    hash1 = compute_safe_hash(parts)
    hash2 = compute_safe_hash(parts)
    assert len(hash1) == 64
    assert hash1 == hash2


def test_compute_safe_hash_differs_on_field_change():
    parts1 = ["FIELD", "customers.xlsx", "Sheet1", "AadhaarNumber", "TEXT", "INDIA_AADHAAR"]
    parts2 = ["FIELD", "customers.xlsx", "Sheet1", "PanNumber", "TEXT", "INDIA_PAN"]
    assert compute_safe_hash(parts1) != compute_safe_hash(parts2)


def test_generate_field_evidence_structure_and_safe_metadata():
    profile = {
        "columnName": "AadhaarNumber",
        "ordinalPosition": 1,
        "inferredDataType": "TEXT",
        "classificationStatus": "CLASSIFIED",
        "classificationCode": "INDIA_AADHAAR",
        "classificationMethod": "HEADER_RULE",
        "matchPercentage": 100.0,
        "privacyCategory": "PERSONAL_DATA",
        "isPersonalData": True,
        "isRegulatedIdentifier": True,
        "nonEmptyCount": 50,
        "emptyCount": 0,
    }

    evidence = generate_field_evidence("customers.xlsx", "Sheet1", profile)

    assert evidence["evidenceReference"] == "EVD-customers.xlsx-COL-AadhaarNumber"
    assert evidence["evidenceType"] == "FIELD_CLASSIFICATION"
    assert evidence["sourceReference"] == "customers.xlsx"
    assert evidence["fieldName"] == "AadhaarNumber"
    assert evidence["classificationCode"] == "INDIA_AADHAAR"
    assert evidence["ruleVersion"] == RULE_VERSION
    assert evidence["agentVersion"] == AGENT_VERSION
    assert evidence["hashAlgorithm"] == HASH_ALGORITHM
    assert len(evidence["evidenceHash"]) == 64

    # Verify metadata JSON
    meta = json.loads(evidence["metadataJson"])
    assert meta["columnName"] == "AadhaarNumber"
    assert meta["isPersonalData"] is True
    assert meta["isRegulatedIdentifier"] is True
    assert meta["nonEmptyCount"] == 50

    # Ensure no raw sample values are in evidence
    raw_sample = "999912345678"
    assert raw_sample not in json.dumps(evidence)


def test_generate_run_evidence():
    summary = {
        "totalColumns": 5,
        "classifiedColumns": 4,
        "unclassifiedColumns": 1,
        "classificationCoveragePercentage": 80.0,
        "totalPersonalDataColumns": 2,
    }

    run_ev = generate_run_evidence("data.csv", None, 100, 5, summary)

    assert run_ev["evidenceReference"] == "EVD-data.csv-RUN"
    assert run_ev["evidenceType"] == "ANALYSIS_RUN"
    assert run_ev["sourceReference"] == "data.csv"
    assert run_ev["fieldName"] is None
    assert run_ev["classificationCode"] is None
    assert len(run_ev["evidenceHash"]) == 64

    meta = json.loads(run_ev["metadataJson"])
    assert meta["rowCount"] == 100
    assert meta["columnCount"] == 5
    assert meta["totalPersonalDataColumns"] == 2
    assert meta["processingLocation"] == "LOCAL_EDGE_AGENT"


def test_generate_inspection_evidence_integration():
    column_profiles = [
        {
            "columnName": "Full_Name",
            "ordinalPosition": 1,
            "inferredDataType": "TEXT",
            "classificationStatus": "CLASSIFIED",
            "classificationCode": "PERSON_NAME",
            "classificationMethod": "HEADER_RULE",
            "matchPercentage": 100.0,
            "privacyCategory": "PERSONAL_DATA",
            "isPersonalData": True,
            "isRegulatedIdentifier": False,
            "nonEmptyCount": 20,
            "emptyCount": 0,
        },
        {
            "columnName": "Amount",
            "ordinalPosition": 2,
            "inferredDataType": "DECIMAL",
            "classificationStatus": "UNCLASSIFIED",
            "classificationCode": None,
            "classificationMethod": "NO_MATCH",
            "matchPercentage": None,
            "privacyCategory": "NOT_PERSONAL",
            "isPersonalData": False,
            "isRegulatedIdentifier": False,
            "nonEmptyCount": 20,
            "emptyCount": 0,
        },
    ]

    summary = {
        "totalColumns": 2,
        "classifiedColumns": 1,
        "unclassifiedColumns": 1,
        "classificationCoveragePercentage": 50.0,
        "totalPersonalDataColumns": 1,
    }

    all_evidence = generate_inspection_evidence(
        file_name="transactions.csv",
        sheet_name=None,
        row_count=20,
        column_count=2,
        column_profiles=column_profiles,
        classification_summary=summary,
    )

    # 1 run evidence + 2 field evidences = 3
    assert len(all_evidence) == 3
    assert all_evidence[0]["evidenceType"] == "ANALYSIS_RUN"
    assert all_evidence[1]["evidenceType"] == "FIELD_CLASSIFICATION"
    assert all_evidence[1]["fieldName"] == "Full_Name"
    assert all_evidence[2]["evidenceType"] == "FIELD_CLASSIFICATION"
    assert all_evidence[2]["fieldName"] == "Amount"
