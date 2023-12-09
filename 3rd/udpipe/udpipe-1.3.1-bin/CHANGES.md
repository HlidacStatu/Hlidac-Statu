Version 1.3.1 [15 Nov 23]
-------------------------
- Update MorphoDiTa to 1.11.2.


Version 1.3.0 [16 Feb 23]
-------------------------
- Get rid of UndefinedBehaviourSanitizer and AddressSanitizer findings.
- Add `segment_size` and `learning_rate_final` parameters to tokenizer training.
- Add several options to `udpipe_server`.
- Fix bug in returning the trained model as a string; use bytes instead.
- Fix a bug that newlines after URL/emails were considered just spaces.
- Fix a silent error on aarch64 caused by assuming char is signed.
- On Windows, the file paths are now UTF-8 encoded, instead of ANSI.
  This change affects the API, binary arguments, and program outputs.
- The Windows binaries are now compiled with VS 2019, older systems
  than Windows 7 are no longer supported.
- Add ARM64 macOS build.
- The Python wheels are provided for Pythons 3.6-3.11.


Version 1.2.0 [01 Aug 17]
-------------------------
- On-demand loading of models in REST server, with a pool
  of least recently used models.
- Make GRU tokenizer dimension configurable (16, 24, 64 supported).
- Track paragraph boundaries even under `normalized_spaces`.
- Support experimental sentence segmentation using
  jointly both the tokenizer and the parser.
- Add EPE output format.
- Make default model in REST server explicit.
- Support pre-filling according to URL params in the webapp.


Version 1.1.0 [29 Mar 17]
-------------------------
- Morphodita_parsito models (now version 3) require at least
  UDPipe version 1.1.0.
- CoNLL-U v2 format is supported. Notably spaces in forms
  and lemmas are now allowed, as are empty nodes.
- Support options for `input_format` and `output_format` instances.
- Preserve all spacing when tokenizing.
- Optionally generate document-level token ranges in the original text.
- Optionally respect given segmentation during tokenization.
- Tokenizer can be trained to allow spaces in tokens
  (default if there are forms with spaces in the training data).
- Parser can be trained to return always one root per sentence (default).
- Improve `input_format` API to allow inter-block state (for correct
  tracking of inter-sentence spaces and document-level offsets).
- Improve `output_format` API to support begin/end document marks
  and to allow state in the `output_format` instance (to allow
  numbering output sentences, for example).


Version 1.0.0 [27 May 16]
-------------------------
- Initial public release.
