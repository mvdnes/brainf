; This program does not make any sense.
; It is just used to determine the opcodes when assembled.

bits 64

global main
extern getchar

main:
    push 0xeadbeef
    call getchar
    push rbx
    pop rbx
    mov edi,eax
    mov rbx,0xdeadbeefcafedeed

.loop1:
    mov [rbx],al
    mov al,[rbx]
    jmp .loop1

    inc rbx
    dec rbx
    inc eax
    dec eax

    xor rax, rax

    cmp al, 0
    je .end
.end:
    
    ret
