"""Prepare pinned PP-OCRv5 Mobile assets for LightTranslator.

This development-only script downloads official PaddlePaddle ONNX models and
the matching official character dictionary, verifies pinned model checksums,
and records source revisions plus runtime asset SHA-256 hashes.
"""

from __future__ import annotations

import hashlib
import importlib.metadata
import os
from pathlib import Path
import shutil
import tempfile
import urllib.request


DETECTION_REPOSITORY = "PaddlePaddle/PP-OCRv5_mobile_det_onnx"
DETECTION_REVISION = "e6f4fa85f00e168c862bc462aebca69eef9b3d3d"
RECOGNITION_REPOSITORY = "PaddlePaddle/PP-OCRv5_mobile_rec_onnx"
RECOGNITION_REVISION = "ed152b8b495f84de93cda5709d768548a9127622"
MODEL_SHA256 = {
    DETECTION_REPOSITORY: (
        "A431985659DC921974177A95ADCFBB90FD9E51989A5E04D70D0B75F597B6E61D"
    ),
    RECOGNITION_REPOSITORY: (
        "DA72DC72CA4DC220DF0DFDE68C1DEDC31C58D3E76A25871122E5056227D50092"
    ),
}
PADDLEOCR_REVISION = "2661c7c0ef5c613e8f93c6e93b2e052399f0f854"
DICTIONARY_URL = (
    "https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/"
    f"{PADDLEOCR_REVISION}/ppocr/utils/dict/ppocrv5_dict.txt"
)

REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
ASSET_DIRECTORY = REPOSITORY_ROOT / "src" / "LightTranslator" / "Assets" / "Ocr"


def require_file(path: Path, description: str) -> Path:
    if not path.is_file() or path.stat().st_size == 0:
        raise RuntimeError(f"{description} is missing or empty")
    return path


def download_model(repository: str, revision: str) -> Path:
    os.environ.setdefault("HF_HUB_DISABLE_XET", "1")
    from huggingface_hub import snapshot_download

    snapshot = snapshot_download(
        repo_id=repository,
        revision=revision,
        allow_patterns=("inference.onnx",),
    )

    model_path = require_file(
        Path(snapshot) / "inference.onnx",
        "Official ONNX model",
    )
    expected_hash = MODEL_SHA256.get(repository)
    if expected_hash is None:
        raise RuntimeError(f"No pinned SHA-256 is configured for {repository}")

    actual_hash = sha256(model_path)
    if actual_hash != expected_hash.upper():
        raise RuntimeError(
            f"ONNX model SHA-256 mismatch for {repository}: "
            f"expected {expected_hash.upper()}, got {actual_hash}"
        )

    return model_path


def download_dictionary(output_path: Path) -> None:
    request = urllib.request.Request(
        DICTIONARY_URL,
        headers={"User-Agent": "LightTranslator-model-preparation"},
    )

    with urllib.request.urlopen(request, timeout=120) as response:
        output_path.write_bytes(response.read())

    require_file(output_path, "PP-OCRv5 character dictionary")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def package_version(name: str) -> str:
    try:
        return importlib.metadata.version(name)
    except importlib.metadata.PackageNotFoundError:
        return "unknown"


def write_notice(hashes: dict[str, str]) -> None:
    notice = f"""# Third-Party Notices

LightTranslator bundles official PP-OCRv5 Mobile ONNX model assets from the
PaddlePaddle project under the Apache License 2.0.

## Sources

- Detection model: https://huggingface.co/{DETECTION_REPOSITORY}/tree/{DETECTION_REVISION}
- Detection revision: `{DETECTION_REVISION}`
- Recognition model: https://huggingface.co/{RECOGNITION_REPOSITORY}/tree/{RECOGNITION_REVISION}
- Recognition revision: `{RECOGNITION_REVISION}`
- Character dictionary: https://github.com/PaddlePaddle/PaddleOCR/blob/{PADDLEOCR_REVISION}/ppocr/utils/dict/ppocrv5_dict.txt
- PaddleOCR revision: `{PADDLEOCR_REVISION}`
- License: Apache License 2.0, https://www.apache.org/licenses/LICENSE-2.0

## Acquisition

- huggingface-hub `{package_version("huggingface-hub")}`
- Upstream ONNX files are downloaded from pinned revisions and verified against
  the expected SHA-256 values before being copied into the application assets.

## Runtime asset SHA-256

| File | SHA-256 |
| --- | --- |
| `ppocrv5_mobile_det.onnx` | `{hashes["ppocrv5_mobile_det.onnx"]}` |
| `ppocrv5_mobile_rec.onnx` | `{hashes["ppocrv5_mobile_rec.onnx"]}` |
| `ppocrv5_dict.txt` | `{hashes["ppocrv5_dict.txt"]}` |
"""

    (ASSET_DIRECTORY / "THIRD-PARTY-NOTICES.md").write_text(
        notice,
        encoding="utf-8",
        newline="\n",
    )


def main() -> None:
    ASSET_DIRECTORY.mkdir(parents=True, exist_ok=True)

    detection_model = download_model(
        DETECTION_REPOSITORY,
        DETECTION_REVISION,
    )
    recognition_model = download_model(
        RECOGNITION_REPOSITORY,
        RECOGNITION_REVISION,
    )

    with tempfile.TemporaryDirectory(prefix="lighttranslator-ocr-") as temporary:
        temporary_directory = Path(temporary)
        prepared_assets = {
            "ppocrv5_mobile_det.onnx": temporary_directory / "detection.onnx",
            "ppocrv5_mobile_rec.onnx": temporary_directory / "recognition.onnx",
            "ppocrv5_dict.txt": temporary_directory / "ppocrv5_dict.txt",
        }

        shutil.copyfile(
            detection_model,
            prepared_assets["ppocrv5_mobile_det.onnx"],
        )
        shutil.copyfile(
            recognition_model,
            prepared_assets["ppocrv5_mobile_rec.onnx"],
        )
        download_dictionary(prepared_assets["ppocrv5_dict.txt"])

        for file_name, prepared_path in prepared_assets.items():
            require_file(prepared_path, file_name)
            shutil.copyfile(prepared_path, ASSET_DIRECTORY / file_name)

    hashes = {
        file_name: sha256(ASSET_DIRECTORY / file_name)
        for file_name in (
            "ppocrv5_mobile_det.onnx",
            "ppocrv5_mobile_rec.onnx",
            "ppocrv5_dict.txt",
        )
    }
    write_notice(hashes)

    print("Prepared PP-OCRv5 Mobile runtime assets:")
    for file_name, checksum in hashes.items():
        print(f"{file_name}: {checksum}")


if __name__ == "__main__":
    main()
