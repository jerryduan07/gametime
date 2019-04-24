/* See the LICENSE file, located in the root directory of
 * the source distribution and
 * at http://verifun.eecs.berkeley.edu/gametime/about/LICENSE,
 * or details on the GameTime license and authors. */


#define LIMIT 100           /* Limit on the final speed. */

/* Saturates the value provided to the range [-limit, limit]. */
int saturate(int value, int limit)
{
    if (value > limit) { return limit; }
    else if (value < -limit) { return -limit; }
    else { return value; }
}

int initial_speed;
short acc;
short time;

/* Calculates the final (one-dimensional) speed of an object with
 * the initial speed (`initial_speed`) and the constant acceleration (`acc`)
 * provided, after a certain amount of time (`time`). The final speed
 * cannot exceed a certain predefined limit. */
int calculate_final_speed()
{
    int final_speed = initial_speed + (acc * time);
    final_speed = saturate(final_speed, LIMIT);
    return final_speed;
}

int main(void)
{
    __gt_simulate();
    return 0;
}
