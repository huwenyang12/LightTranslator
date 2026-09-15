from __future__ import annotations

import hashlib
import importlib.util
from pathlib import Path
import sys
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

MODULE_PATH = Path(__file__).with_name("prepare_ocr_models.py")
MODULE_SPEC = importlib.util.spec_from_file_location("prepare_ocr_models", MODULE_PATH)
if MODULE_SPEC is None or MODULE_SPEC.loader is None:
    raise RuntimeError(f"Unable to load {MODULE_PATH}")

prepare_ocr_models = importlib.util.module_from_spec(MODULE_SPEC)
MODULE_SPEC.loader.exec_module(prepare_ocr_models)


class DownloadModelTests(unittest.TestCase):
    def test_download_model_returns_verified_onnx_file(self) -> None:
        model_bytes = b"official-onnx-model"
        expected_hash = hashlib.sha256(model_bytes).hexdigest().upper()

        with tempfile.TemporaryDirectory() as temporary:
            snapshot = Path(temporary)
            (snapshot / "inference.json").write_text("{}", encoding="utf-8")
            (snapshot / "inference.pdiparams").write_bytes(b"legacy")
            model_path = snapshot / "inference.onnx"
            model_path.write_bytes(model_bytes)
            calls: list[dict[str, object]] = []

            def snapshot_download(**kwargs: object) -> str:
                calls.append(kwargs)
                return str(snapshot)

            fake_hub = SimpleNamespace(snapshot_download=snapshot_download)
            with (
                patch.dict(sys.modules, {"huggingface_hub": fake_hub}),
                patch.object(
                    prepare_ocr_models,
                    "MODEL_SHA256",
                    {"example/model": expected_hash},
                    create=True,
                ),
            ):
                actual = prepare_ocr_models.download_model(
                    "example/model",
                    "0123456789abcdef",
                )

            self.assertEqual(model_path, actual)
            self.assertEqual(("inference.onnx",), calls[0]["allow_patterns"])

    def test_download_model_rejects_unexpected_content(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            snapshot = Path(temporary)
            (snapshot / "inference.json").write_text("{}", encoding="utf-8")
            (snapshot / "inference.pdiparams").write_bytes(b"legacy")
            (snapshot / "inference.onnx").write_bytes(b"tampered")

            fake_hub = SimpleNamespace(
                snapshot_download=lambda **_: str(snapshot),
            )
            with (
                patch.dict(sys.modules, {"huggingface_hub": fake_hub}),
                patch.object(
                    prepare_ocr_models,
                    "MODEL_SHA256",
                    {"example/model": "0" * 64},
                    create=True,
                ),
            ):
                with self.assertRaisesRegex(RuntimeError, "SHA-256 mismatch"):
                    prepare_ocr_models.download_model(
                        "example/model",
                        "0123456789abcdef",
                    )


if __name__ == "__main__":
    unittest.main()
