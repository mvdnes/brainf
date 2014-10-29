#include <iostream>
#include <fstream>
#include <cstdlib>
#include <string>
#include <cstring>
#include <vector>

using namespace std;

typedef char bf_t;

bf_t bfmem[30000];

void execute(string program, bf_t* ptr) {
    int loc = 0;
    vector<int> stack;
    
    while (true) {
        if (loc >= (int)program.size()) break;
        
        switch (program[loc]) {
        default:
            break;
        case '>':
            ++ptr;
            break;
        case '<':
            --ptr;
            break;
        case '+':
            ++(*ptr);
            break;
        case '-':
            --(*ptr);
            break;
        case '.':
            cout.put((char)*ptr);
            break;
        case ',':
            *ptr = cin.get();
            break;
        case '[':
            stack.push_back(loc);
            if (*ptr == 0) {
                stack.pop_back();
                int depth = 0;
                while (loc < (int)program.size()) {
                    if (program[loc] == '[') depth++;
                    else if (program[loc] == ']' && depth == 1) break;
                    else if (program[loc] == ']') depth--;
                    ++loc;
                }
            }
            break;
        case ']':
            if (*ptr != 0) {
                loc = stack.back();
            } else {
                stack.pop_back();
            }
            break;
        }
        ++loc;
    }
}

int main(int argc, char* argv[]) {
    if (argc < 2) {
        cerr << "Usage: " << argv[0] << " <program>" << endl;
        return 1;
    }
    
    ifstream bf(argv[1]);
    if (!bf.is_open()) {
        cerr << "could not open file." << endl;
        return 2;
    }
    
    string program((std::istreambuf_iterator<char>(bf)),
                 std::istreambuf_iterator<char>());
    
    execute(program, &bfmem[0]);
    
    return 0;
}
