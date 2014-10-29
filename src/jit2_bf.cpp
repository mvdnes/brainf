#include <vector>
#include <string>
#include <fstream>
#include <iostream>
#include <cstdlib>
#include <cstring>
#include <cstdio>
#include <sys/mman.h>

using namespace std;

struct bfopcode
{
    char type;
    int count;
    bfopcode(char t, int c) { type = t; count = c; }
};

typedef unsigned long long uint64_t;
typedef unsigned char bf_t;
typedef vector<unsigned char> vcode;

bf_t bfmem[30000];

char getchar_fixed()
{
    char c = getchar();
    if (c == EOF) return 0;
    return c;
}

void compile_read(vcode &code)
{
    // mov rax imm64
    code.push_back(0x48); // 64 bit prefix
    code.push_back(0xB8); // mov .ax imm

    uint64_t fptr = (uint64_t)getchar_fixed;
    for (unsigned i = 0; i < 8; ++i)
    {
        code.push_back((unsigned char)(fptr >> (i * 8)));
    }

    // call rax
    code.push_back(0xFF);
    code.push_back(0xD0);

    // mov [rbx],al
    code.push_back(0x88);
    code.push_back(0x03);
}

void compile_write(vcode &code)
{
    // mov edi [rbx]
    code.push_back(0x8B);
    code.push_back(0x3B);

    // mov rax imm
    code.push_back(0x48); // 64 bit prefix
    code.push_back(0xB8); // mov .ax imm

    uint64_t fptr = (uint64_t)putchar;
    for (unsigned i = 0; i < 8; ++i)
    {
        code.push_back((unsigned char)(fptr >> (i * 8)));
    }

    // call rax
    code.push_back(0xFF);
    code.push_back(0xD0);
}

void compile_right(vcode &code, int count)
{
    // add rbx imm32
    code.push_back(0x48); // 64bit
    code.push_back(0x81);
    code.push_back(0xC3);

    for (unsigned i = 0; i < 4; ++i) {
        code.push_back(count >> (i * 8));
    }
}

void compile_plus(vcode &code, char count)
{
    // add [rbx] imm8
    code.push_back(0x80);
    code.push_back(0x03);
    code.push_back(count);
}

unsigned compile_loopstart(vcode &code)
{
    // we need to jump to here
    unsigned branch_start = code.size();

    // compare byte [rbx] with 0
    code.push_back(0x80);
    code.push_back(0x3B);
    code.push_back(0x00);
    
    // jump if equal
    code.push_back(0x0F);
    code.push_back(0x84);

    // jump destination placeholder
    code.push_back(0);
    code.push_back(0);
    code.push_back(0);
    code.push_back(0);

    // loop offset = 3 + 2 + 4

    return branch_start;
}

void compile_loopend(vcode &code, unsigned loopstart)
{
    // jmp rel32
    unsigned loopstart_rel = loopstart - (code.size() + 5u);
    code.push_back(0xE9);
    for (int i = 0; i < 4; ++i) {
        code.push_back(loopstart_rel >> (i * 8));
    }

    // set the jump destination at the loop start
    int loopend_rel = code.size() - (loopstart + 3 + 2 + 4);
    for (int i = 0; i < 4; ++i) {
        code[loopstart + 3 + 2 + i] = (loopend_rel >> (i * 8));
    }
}

int compile(const vector<bfopcode> &program, vcode &code)
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

    vector<unsigned> stack;
    for (size_t pc = 0; pc < program.size(); ++pc)
    {
        switch (program[pc].type) {
        default:
            break;
        case '.':
            compile_write(code);
            break;
        case ',':
            compile_read(code);
            break;
        case '>':
            compile_right(code, program[pc].count);
            break;
        case '+':
            compile_plus(code, program[pc].count);
            break;
        case '[':
            stack.push_back(compile_loopstart(code));
            break;
        case ']':
            if (stack.size() == 0) return 3;
            compile_loopend(code, stack.back());
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

void change_count_or_push(char type, int delta, bfopcode &cur, vector<bfopcode> &result)
{
    if (cur.type == type && delta != 0) cur.count += delta;
    else {
        if (cur.type != 0) result.push_back(cur);
        cur = bfopcode(type, delta == 0 ? 1 : delta);
    }
}

vector<bfopcode> simplify_code(const string &code)
{
    vector<bfopcode> result;
    bfopcode cur = bfopcode(0, 0);
    for (size_t pc = 0; pc < code.size(); ++pc)
    {
        switch (code[pc])
        {
        case '+':
            change_count_or_push('+', 1, cur, result);
            break;
        case '-':
            change_count_or_push('+', -1, cur, result);
            break;
        case '>':
            change_count_or_push('>', 1, cur, result);
            break;
        case '<':
            change_count_or_push('>', -1, cur, result);
            break;
        case '.':
            change_count_or_push('.', 0, cur, result);
            break;
        case ',':
            change_count_or_push(',', 0, cur, result);
            break;
        case '[':
            change_count_or_push('[', 0, cur, result);
            break;
        case ']':
            change_count_or_push(']', 0, cur, result);
            break;
        default:
            break;
        }
    }
    if (cur.type != 0) {
        result.push_back(cur);
    }
    return result;
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

    vector<bfopcode> preprocessed_program = simplify_code(program);

    int status = compile(preprocessed_program, code);
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
