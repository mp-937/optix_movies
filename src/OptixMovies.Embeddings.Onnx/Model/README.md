# Embedding model

[bge-small-en-v1.5](https://huggingface.co/BAAI/bge-small-en-v1.5) by BAAI, quantised to 8 bits, as `OnnxTextEmbedder` runs it. MIT licence, in `LICENSE`.

| File | Source |
| --- | --- |
| `model.onnx` | `onnx/model_quantized.onnx` from [Xenova/bge-small-en-v1.5](https://huggingface.co/Xenova/bge-small-en-v1.5/tree/ea104dacec62c0de699686887e3f920caeb4f3e3), Hugging Face's ONNX conversion. SHA-256 `6c9c6101a956d62dfb5e7190c538226c0c5bb9cb27b651234b6df063ee7dbfe4` |
| `vocab.txt` | [BAAI/bge-small-en-v1.5](https://huggingface.co/BAAI/bge-small-en-v1.5/tree/5c38ec7c405ec4b44b94cc5a9bb96e735b38267a) |
| `LICENSE` | [FlagEmbedding](https://github.com/FlagOpen/FlagEmbedding), BAAI's repository for the model |

Of the 8-bit builds on offer, this one stays closest to the full-precision model: over this dataset's movies, the two models' vectors have an average cosine similarity of 0.996, and 9 of the top 10 results for sample searches match on average.
