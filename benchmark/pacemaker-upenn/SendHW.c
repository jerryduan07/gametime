#include "SendHW.h"

#include <include.h>


void  Send_VP1() {
	P5OUT |=0x02;
	////////////////////////		P5OUT = (P5OUT&0xFD)|(VP<<1);
}

void  Send_VS1() {
	P4OUT |=0x08;
}

void  Send_AP1() {
	P5OUT |= 0x01;
	///////////////////////		P5OUT = (P5OUT&0xFE)|AP;
}

void  Send_AS1() {
	P4OUT |=0x04;
}
