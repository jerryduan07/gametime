# 1 "/home/elect/gametime-1.5/sandbox/modexp/modexp_simple-gt/modexp_simple-gt.c"
# 1 "<built-in>"
# 1 "<command-line>"
# 1 "C:/cygwin64/home/elect/gametime-1.5/sandbox/modexp/modexp_simple-gt/modexp_simple-gt.c"
# 14 "C:/cygwin64/home/elect/gametime-1.5/sandbox/modexp/modexp_simple-gt/modexp_simple-gt.c"
unsigned int base;
unsigned int exponent;
unsigned int result;


void modexp_simple(void)
{
    base = 2;
    result = 1;

    for (int i = 0; i < 4; i++)
    {
        if ((exponent & 1) == 1)
        {
            result = (result * base) % 1048583;
        }
        exponent >>= 1;
        base = (base * base) % 1048583;
    }
}

int main(void)
{
    __gt_simulate();
    return 0;
}
