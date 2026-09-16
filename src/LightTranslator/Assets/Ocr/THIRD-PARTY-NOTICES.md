# Third-Party Notices

LightTranslator bundles official PP-OCRv5 Mobile ONNX model assets from the
PaddlePaddle project under the Apache License 2.0.

## Sources

- Detection model: https://huggingface.co/PaddlePaddle/PP-OCRv5_mobile_det_onnx/tree/e6f4fa85f00e168c862bc462aebca69eef9b3d3d
- Detection revision: `e6f4fa85f00e168c862bc462aebca69eef9b3d3d`
- Recognition model: https://huggingface.co/PaddlePaddle/PP-OCRv5_mobile_rec_onnx/tree/ed152b8b495f84de93cda5709d768548a9127622
- Recognition revision: `ed152b8b495f84de93cda5709d768548a9127622`
- Character dictionary: https://github.com/PaddlePaddle/PaddleOCR/blob/2661c7c0ef5c613e8f93c6e93b2e052399f0f854/ppocr/utils/dict/ppocrv5_dict.txt
- PaddleOCR revision: `2661c7c0ef5c613e8f93c6e93b2e052399f0f854`
- License: Apache License 2.0, https://www.apache.org/licenses/LICENSE-2.0

## Acquisition

- huggingface-hub `0.34.4`
- Upstream ONNX files are downloaded from pinned revisions and verified against
  the expected SHA-256 values before being copied into the application assets.

## Runtime asset SHA-256

| File | SHA-256 |
| --- | --- |
| `ppocrv5_mobile_det.onnx` | `A431985659DC921974177A95ADCFBB90FD9E51989A5E04D70D0B75F597B6E61D` |
| `ppocrv5_mobile_rec.onnx` | `DA72DC72CA4DC220DF0DFDE68C1DEDC31C58D3E76A25871122E5056227D50092` |
| `ppocrv5_dict.txt` | `D1979E9F794C464C0D2E0B70A7FE14DD978E9DC644C0E71F14158CDF8342AF1B` |
