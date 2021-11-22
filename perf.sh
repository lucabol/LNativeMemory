#!/usr/bin/bash
mkdir -p tmp
cp csharp/bin/Release/net6.0/linux-x64/publish/csharp tmp/csharp-aot
cp rust/target/release/rust tmp/rust
strip tmp/csharp-aot
strip tmp/rust
hyperfine -i -L elms 100,1000 -L iters 1000,100000 --export-markdown result.mkd 'tmp/rust {elms} {iters}' 'tmp/csharp-aot {elms} {iters}' 'csharp/bin/Release/net6.0/csharp {elms} {iters}'
echo
echo ----------------------------------------------------------------------
echo
ls -lh tmp
cat result.mkd | mdless -P
