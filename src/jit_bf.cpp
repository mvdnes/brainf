#include <vector>
#include <string>
#include <fstream>
#include <iostream>
#include <cstdlib>
#include <cstring>
#include <cstdio>
#include <sys/mman.h>

using namespace std;

typedef unsigned long long uint64_t;
typedef unsigned char bf_t;
typedef vector<unsigned char> vcode;

bf_t bfmem[30000];

void compile_load(vcode &code, bool &inreg)
{
    if (inreg) return;

    // mov al,[rbx]
    code.push_back(0x8A);
    code.push_back(0x03);

    inreg = true;
}

void compile_store(vcode &code, bool &inreg)
{
    if (!inreg) return;

    // mov [rbx],al
    code.push_back(0x88);
    code.push_back(0x03);

    inreg = false;
}

void compile_sync(vcode &code, bool &inreg)
{
    if (inreg) {
        compile_store(code, inreg);
    }
    else {
        compile_load(code, inreg);
    }

    inreg = true;
}

void compile_read(vcode &code, bool &inreg)
{
    // mov rax imm64
    code.push_back(0x48); // 64 bit prefix
    code.push_back(0xB8); // mov .ax imm

    uint64_t fptr = (uint64_t)getchar;
    for (unsigned i = 0; i < 8; ++i)
    {
        code.push_back((unsigned char)(fptr >> (i * 8)));
    }

    // call rax
    code.push_back(0xFF);
    code.push_back(0xD0);

    // the result of the call will be in eax, which means inreg=true
    inreg = true;
}

void compile_write(vcode &code, bool &inreg)
{
    // ensure al and [rbx] are equal
    // we need al for the call, but it will also be overwritten by the call
    compile_sync(code, inreg);

    // mov rdi,rax
    code.push_back(0x89);
    code.push_back(0xC7);

    // mov rax imm
    code.push_back(0x48); // 64 bit prefix
    code.push_back(0xB8); // mov .ax imm

    inreg = false; // We have just overwritten rax

    uint64_t fptr = (uint64_t)putchar;
    for (unsigned i = 0; i < 8; ++i)
    {
        code.push_back((unsigned char)(fptr >> (i * 8)));
    }

    // call rax
    code.push_back(0xFF);
    code.push_back(0xD0);
}

void compile_right(vcode &code, bool &inreg)
{
    compile_store(code, inreg);

    // inc rbx
    code.push_back(0x48); // 64bit
    code.push_back(0xFF);
    code.push_back(0xC3);
}

void compile_left(vcode &code, bool &inreg)
{
    compile_store(code, inreg);

    // dec rbx
    code.push_back(0x48); // 64bit
    code.push_back(0xFF);
    code.push_back(0xCB);
}

void compile_plus(vcode &code, bool &inreg)
{
    compile_load(code, inreg);
  
    // inc eax
    code.push_back(0xFF);
    code.push_back(0xC0);
}

void compile_minus(vcode &code, bool &inreg)
{
    compile_load(code, inreg);

    // dec eax
    code.push_back(0xFF);
    code.push_back(0xC8);
}

unsigned compile_loopstart(vcode &code, bool &inreg)
{
    compile_load(code, inreg);
    
    // we need to jump to here
    unsigned branch_start = code.size();

    // compare al with 0
    code.push_back(0x3C);
    code.push_back(0x00);
    
    // jump if equal
    code.push_back(0x0F);
    code.push_back(0x84);

    // jump destination placeholder
    code.push_back(0);
    code.push_back(0);
    code.push_back(0);
    code.push_back(0);

    // loop offset = 2+2+4

    return branch_start;
}

void compile_loopend(vcode &code, bool &inreg, unsigned loopstart)
{
    compile_load(code, inreg);
    
    // jmp rel32
    unsigned loopstart_rel = loopstart - (code.size() + 5u);
    code.push_back(0xE9);
    for (int i = 0; i < 4; ++i) {
        code.push_back(loopstart_rel >> (i * 8));
    }

    // set the jump destination at the loop start
    int loopend_rel = code.size() - (loopstart + 2 + 2 + 4);
    for (int i = 0; i < 4; ++i) {
        code[loopstart + 2 + 2 + i] = (loopend_rel >> (i * 8));
    }
}

int compile(const string &program, vcode &code)
{
    code.clear();

    // push rbx
    code.push_back(0x53);

    // load rbx with imm64
    code.push_back(0x48);
    code.push_back(0xBB);
    uint64_t ptr = (uint64_t)&bfmem[0];
    for (unsigned i = 0; i < 8; ++i) {
        code.push_back((unsigned char)((ptr >> (i * 8)) & 0xFF));
    }

    bool inreg = false;
    vector<unsigned> stack;
    for (size_t pc = 0; pc < program.size(); ++pc)
    {
        switch (program[pc]) {
        default:
            break;
        case '.':
            compile_write(code, inreg);
            break;
        case ',':
            compile_read(code, inreg);
            break;
        case '>':
            compile_right(code, inreg);
            break;
        case '<':
            compile_left(code, inreg);
            break;
        case '+':
            compile_plus(code, inreg);
            break;
        case '-':
            compile_minus(code, inreg);
            break;
        case '[':
            stack.push_back(compile_loopstart(code, inreg));
            break;
        case ']':
            if (stack.size() == 0) return 3;
            compile_loopend(code, inreg, stack.back());
            stack.pop_back();
            break;
        }
    }

    // pop rbx
    code.push_back(0x5B);

    // ret
    code.push_back(0xC3);

    if (stack.size() != 0) return 3;

    return 0;
}

int main(int argc, char *argv[]) {
    if (argc < 2) {
        cerr << "Usage: " << argv[0] << " <filename>" << endl;
        return 1;
    }

    vector<unsigned char> code;

    ifstream bf(argv[1]);
    if (!bf.is_open()) {
        cerr << "could not open file." << endl;
        return 2;
    }

    string program((std::istreambuf_iterator<char>(bf)),
            std::istreambuf_iterator<char>());
    bf.close();

    int status = compile(program, code);
    if (status != 0) {
        cerr << "Error in bf program" << endl;
        return status;
    }

    // Allocate writable/executable memory.
    void *mem = mmap(NULL, code.size(), PROT_WRITE | PROT_EXEC,
                     MAP_ANON | MAP_PRIVATE, -1, 0);
    memcpy(mem, &code[0], code.size());
    mprotect(mem, code.size(), PROT_EXEC);

    // Create a function pointer to the memory and call it
    void (*func)() = (void(*)())mem;
    func();

    return 0;
}
