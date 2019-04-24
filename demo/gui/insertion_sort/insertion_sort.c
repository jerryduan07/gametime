/* This demonstration file performs insertion sort on an array of fixed size */


/* See the LICENSE file, located in the root directory of
 * the source distribution and
 * at http://verifun.eecs.berkeley.edu/gametime/about/LICENSE,
 * or details on the GameTime license and authors. */


#define LENGTH 8
int a[LENGTH];
int i;
int index = 0;


// insertion sort
void insertion_sort(void) {
    for (i = 1; i < LENGTH; i++) {
      index = i;
      while (index >= 1 && a[index] < a[index - 1]) {
        int tmp = a[index];
        a[index] = a[index - 1];
        a[index - 1] = tmp;
        index--;
      }
    }
    
}

int main(void) {
    __gt_simulate();
    return 0;
}
