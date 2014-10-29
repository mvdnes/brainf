#!/bin/bash

# we assemble and link the findopcode program
# afterwards we disassemble the binary to view the byte-codes

set -e

nasm -f elf64 findopcode.asm -o findopcode.o
gcc findopcode.o -m64
objdump -D -b binary -m i386:x86-64 a.out | less
