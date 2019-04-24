/* This demonstration file performs modular exponentation, where
 * a base is raised to a four-bit exponent, modulo a large prime number.
 * The exponentiation is performed with the square-and-multiply method. */

/* See the LICENSE file, located in the root directory of
 * the source distribution and
 * at http://verifun.eecs.berkeley.edu/gametime/about/LICENSE,
 * or details on the GameTime license and authors. */


#define EXP_BITS 4          /* Number of bits in the exponent. */
#define PRIME 1048583       /* Next prime number after 2^20. */

unsigned int base;
unsigned int exponent;
unsigned int result;

/* Computes (base)^(exponent) (mod PRIME). */
void modexp_simple(void)
{
    base = 2;
    result = 1;

    /* Loop unrolled for EXP_BITS = 4. */
    if ((exponent & 1) == 1)
    {
        result = (result * base) % PRIME;
    }
    exponent >>= 1;
    base = (base * base) % PRIME;

    if ((exponent & 1) == 1)
    {
        result = (result * base) % PRIME;
    }
    exponent >>= 1;
    base = (base * base) % PRIME;

    if ((exponent & 1) == 1)
    {
        result = (result * base) % PRIME;
    }
    exponent >>= 1;
    base = (base * base) % PRIME;

    if ((exponent & 1) == 1)
    {
        result = (result * base) % PRIME;
    }
    exponent >>= 1;
    base = (base * base) % PRIME;
}

int main(void)
{
    __gt_simulate();
    return 0;
}
