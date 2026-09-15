"""Prepare pinned PP-OCRv5 Mobile assets for LightTranslator.

This development-only script downloads official PaddlePaddle inference models,
converts them to ONNX, downloads the matching official character dictionary,
and records source revisions plus output SHA-256 hashes.
"""

from __future__ import annotations

import hashlib
import importlib.metadata
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import urllib.request


DETECTION_REPOSITORY = "PaddlePaddle/PP-OCRv5_mobile_det"
DETECTION_REVISION = "0d63e78e2b680928f6b1747d76a08db6e645efb7"
RECOGNITION_REPOSITORY = "PaddlePaddle/PP-OCRv5_mobile_rec"
RECOGNITION_REVISION = "682f20538d8c086cb2128e5cfac775e6c4904e85"
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
        allow_patterns=(
            "inference.json",
            "inference.pdiparams",
        ),
    )

    model_directory = Path(snapshot)
    require_file(model_directory / "inference.json", "Paddle model structure")
    require_file(model_directory / "inference.pdiparams", "Paddle model parameters")
    return model_directory


def find_converter() -> Path:
    executable_directory = Path(sys.executable).resolve().parent

    for file_name in ("paddle2onnx.exe", "paddle2onnx"):
        candidate = executable_directory / file_name
        if candidate.is_file():
            return candidate

    converter = shutil.which("paddle2onnx")
    if converter is not None:
        return Path(converter)

    raise RuntimeError(
        "paddle2onnx executable was not found; install the pinned model tools first"
    )


def convert_model(model_directory: Path, output_path: Path) -> None:
    converter = find_converter()

    subprocess.run(
        [
            str(converter),
            "--model_dir",
            str(model_directory),
            "--model_filename",
            "inference.json",
            "--params_filename",
            "inference.pdiparams",
            "--save_file",
            str(output_path),
            "--opset_version",
            "11",
            "--enable_onnx_checker",
            "True",
        ],
        check=True,
    )

    require_file(output_path, "Converted ONNX model")


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

LightTranslator bundles converted PP-OCRv5 Mobile model assets from the
PaddlePaddle project under the Apache License 2.0.

## Sources

- Detection model: https://huggingface.co/{DETECTION_REPOSITORY}/tree/{DETECTION_REVISION}
- Detection revision: `{DETECTION_REVISION}`
- Recognition model: https://huggingface.co/{RECOGNITION_REPOSITORY}/tree/{RECOGNITION_REVISION}
- Recognition revision: `{RECOGNITION_REVISION}`
- Character dictionary: https://github.com/PaddlePaddle/PaddleOCR/blob/{PADDLEOCR_REVISION}/ppocr/utils/dict/ppocrv5_dict.txt
- PaddleOCR revision: `{PADDLEOCR_REVISION}`
- License: Apache License 2.0, https://www.apache.org/licenses/LICENSE-2.0

## Conversion tools

- paddlepaddle `{package_version("paddlepaddle")}`
- paddle2onnx `{package_version("paddle2onnx")}`
- huggingface-hub `{package_version("huggingface-hub")}`
- ONNX opset: `11`
- ONNX checker: enabled

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

    detection_directory = download_model(
        DETECTION_REPOSITORY,
        DETECTION_REVISION,
    )
    recognition_directory = download_model(
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

        convert_model(
            detection_directory,
            prepared_assets["ppocrv5_mobile_det.onnx"],
        )
        convert_model(
            recognition_directory,
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
