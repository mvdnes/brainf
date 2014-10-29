#!/bin/bash

HEAD='
#include <iostream>
#include <cstdio>

using namespace std;

typedef char bf_t;

bf_t mem[30000];

char getchar_fixed()
{
    char c = getchar();
    if (c == EOF) return 0;
    return c;
}

int main() {
    bf_t *ptr = &mem[0];
'

TAIL='
    return 0;
}
'

INPUT=$1

TMP=bf_tmp.cpp
echo "$HEAD" > $TMP

while IFS= read -r -n1 char
do
    # display one character at a time
    case  "$char" in
        ">") echo " ++ptr;" >> $TMP;;
        "<") echo " --ptr;" >> $TMP;;
        "+") echo " ++(*ptr);" >> $TMP;;
        "-") echo " --(*ptr);" >> $TMP;;
        ".") echo " cout.put((char)*ptr);" >> $TMP;;
        ",") echo " *ptr = getchar_fixed();" >> $TMP;;
        "[") echo " while(*ptr) {" >> $TMP;;
        "]") echo " }" >> $TMP;;
    esac
done < "$INPUT"

echo "$TAIL" >> $TMP

g++ -Wall -Wextra -o $2 -O2 $TMP

rm $TMP
