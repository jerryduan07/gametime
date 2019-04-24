/*
 * File: CodeExt_DDD_PMbuffer_rev3.c
 *
 * Real-Time Workshop code generated for Simulink model CodeExt_DDD_PMbuffer_rev3.
 *
 * Model version                        : 1.160
 * Real-Time Workshop file version      : 7.4  (R2009b)  29-Jun-2009
 * Real-Time Workshop file generated on : Mon Oct 10 13:40:10 2011
 * TLC version                          : 7.4 (Jul 14 2009)
 * C/C++ source code generated on       : Mon Oct 10 13:40:11 2011
 *
 * Target selection: ert.tlc
 * Embedded hardware selection: Texas Instruments->MSP430
 * Code generation objectives: Unspecified
 * Validation result: Not run
 */

#include "CodeExt_DDD_PMbuffer_rev3.h"
#include "CodeExt_DDD_PMbuffer_rev3_private.h"

/* Named constants for block: '<Root>/Chart1' */
#define event_Vin                      (0)
#define event_Ain                      (1)
#define event_a_p                      (2)
#define event_a_s                      (3)
#define event_v_p                      (4)
#define event_v_s                      (5)
#define event_uri_s                    (6)
#define event_tt                       (7)
#define event_clk                      (8)
#define IN_NO_ACTIVE_CHILD             (0)
#define IN_st3C_CC                     (4)
#define IN_st3                         (3)
#define IN_st2                         (2)
#define IN_st1                         (1)
#define IN_LRI_AS                      (2)
#define IN_LRI                         (1)
#define IN_ARPst2                      (2)
#define IN_inter_CC                    (3)
#define IN_ARPst1                      (1)
#define IN_VRPst2                      (2)
#define IN_VRPst1                      (1)
#define IN_URIst2                      (2)
#define IN_URIst1                      (1)
#define IN_l0                          (1)

int16_T _sfEvent_;

/* Exported block signals */
boolean_T Vin;                         /* '<Root>/In1' */
boolean_T Ain;                         /* '<Root>/In2' */
boolean_T clk_in;                      /* '<Root>/In3' */
boolean_T AP;                          /* '<Root>/Chart1' */
boolean_T AS;                          /* '<Root>/Chart1' */
boolean_T VP;                          /* '<Root>/Chart1' */
boolean_T VS;                          /* '<Root>/Chart1' */

/* Block states (auto storage) */
D_Work rtDWork;

/* Previous zero-crossings (trigger) states */
PrevZCSigStates rtPrevZCSigState;

/* Real-time model */
RT_MODEL rtM_;
RT_MODEL *rtM = &rtM_;
static void c2_CodeExt_DDD_PMbuffer_rev3(void);
static void broadcast_tt(void);

/* Functions for block: '<Root>/Chart1' */
static void c2_CodeExt_DDD_PMbuffer_rev3(void)
{
  int16_T sf_previousEvent;

  /* During: Chart1 */
  if (_sfEvent_ == event_clk) {
    if (rtDWork.temporalCounter_i1 < MAX_uint32_T) {
      rtDWork.temporalCounter_i1 = rtDWork.temporalCounter_i1 + 1UL;
    }

    if (rtDWork.temporalCounter_i2 < MAX_uint32_T) {
      rtDWork.temporalCounter_i2 = rtDWork.temporalCounter_i2 + 1UL;
    }

    if (rtDWork.temporalCounter_i3 < MAX_uint32_T) {
      rtDWork.temporalCounter_i3 = rtDWork.temporalCounter_i3 + 1UL;
    }

    if (rtDWork.temporalCounter_i4 < MAX_uint32_T) {
      rtDWork.temporalCounter_i4 = rtDWork.temporalCounter_i4 + 1UL;
    }

    if (rtDWork.temporalCounter_i5 < MAX_uint32_T) {
      rtDWork.temporalCounter_i5 = rtDWork.temporalCounter_i5 + 1UL;
    }
  }

  /* During 'PAVI': '<S1>:29' */
  if (rtDWork.bitsForTID0.is_active_PAVI != 0) {
    switch (rtDWork.bitsForTID0.is_PAVI) {
     case IN_st1:
      /* During 'st1': '<S1>:34' */
      if ((_sfEvent_ == event_a_p) || (_sfEvent_ == event_a_s)) {
        /* Transition: '<S1>:41' */
        /* Exit 'st1': '<S1>:34' */
        rtDWork.n_t = 0;

        /* Entry 'st2': '<S1>:33' */
        rtDWork.bitsForTID0.is_PAVI = (uint8_T)IN_st2;
        rtDWork.temporalCounter_i1 = 0UL;
      }
      break;

     case IN_st2:
      /* During 'st2': '<S1>:33' */
      if ((rtDWork.sent == 0) && (rtDWork.comm == 0U) &&
          (rtDWork.bitsForTID0.URIex == 0) && (rtDWork.temporalCounter_i1 ==
           (uint32_T)(rtDWork.n_t + rtDWork.AVId))) {
        /* Transition: '<S1>:38' */
        /* Exit 'st2': '<S1>:33' */
        /* Entry 'st3': '<S1>:32' */
        rtDWork.bitsForTID0.is_PAVI = (uint8_T)IN_st3;
      } else if ((rtDWork.sent == 0) && (rtDWork.comm == 0U) &&
                 (rtDWork.bitsForTID0.URIex == 1) && (rtDWork.temporalCounter_i1
                  == (uint32_T)(rtDWork.n_t + rtDWork.AVId))) {
        /* Transition: '<S1>:39' */
        /* Exit 'st2': '<S1>:33' */
        rtDWork.sent = 3;

        /* Entry 'st1': '<S1>:34' */
        rtDWork.bitsForTID0.is_PAVI = (uint8_T)IN_st1;
      } else {
        if (_sfEvent_ == event_v_s) {
          /* Transition: '<S1>:40' */
          /* Exit 'st2': '<S1>:33' */
          /* Entry 'st1': '<S1>:34' */
          rtDWork.bitsForTID0.is_PAVI = (uint8_T)IN_st1;
        }
      }
      break;

     case IN_st3:
      /* During 'st3': '<S1>:32' */
      if (_sfEvent_ == event_uri_s) {
        /* Transition: '<S1>:37' */
        /* Exit 'st3': '<S1>:32' */
        rtDWork.comm = rtDWork.comm + 1U;
        rtDWork.sh_rst = 1U;

        /* Entry 'st3C_CC': '<S1>:31' */
        rtDWork.bitsForTID0.is_PAVI = (uint8_T)IN_st3C_CC;
      }
      break;

     case IN_st3C_CC:
      /* During 'st3C_CC': '<S1>:31' */
      if (rtDWork.sent == 0) {
        /* Transition: '<S1>:36' */
        /* Exit 'st3C_CC': '<S1>:31' */
        rtDWork.sent = 3;
        rtDWork.sh_rst = 1U;
        rtDWork.comm = (rtDWork.comm - 1U);

        /* Entry 'st1': '<S1>:34' */
        rtDWork.bitsForTID0.is_PAVI = (uint8_T)IN_st1;
      }
      break;

     default:
      rtDWork.bitsForTID0.is_PAVI = (uint8_T)IN_NO_ACTIVE_CHILD;
      break;
    }
  }

  /* During 'PLRI': '<S1>:43' */
  if (rtDWork.bitsForTID0.is_active_PLRI != 0) {
    switch (rtDWork.bitsForTID0.is_PLRI) {
     case IN_LRI:
      /* During 'LRI': '<S1>:46' */
      if ((rtDWork.sent == 0) && (rtDWork.comm == 0U) &&
          (rtDWork.temporalCounter_i2 == (uint32_T)((rtDWork.n_t_n +
             rtDWork.LRId) - rtDWork.AVId))) {
        /* Transition: '<S1>:51' */
        /* Exit 'LRI': '<S1>:46' */
        rtDWork.sent = 1;
        rtDWork.n_t_n = 0;

        /* Entry 'LRI': '<S1>:46' */
        rtDWork.bitsForTID0.is_PLRI = (uint8_T)IN_LRI;
        rtDWork.temporalCounter_i2 = 0UL;
      } else if ((_sfEvent_ == event_v_p) || (_sfEvent_ == event_v_s)) {
        /* Transition: '<S1>:53' */
        /* Exit 'LRI': '<S1>:46' */
        rtDWork.n_t_n = 0;

        /* Entry 'LRI': '<S1>:46' */
        rtDWork.bitsForTID0.is_PLRI = (uint8_T)IN_LRI;
        rtDWork.temporalCounter_i2 = 0UL;
      } else {
        if (_sfEvent_ == event_a_s) {
          /* Transition: '<S1>:50' */
          /* Exit 'LRI': '<S1>:46' */
          /* Entry 'LRI_AS': '<S1>:45' */
          rtDWork.bitsForTID0.is_PLRI = (uint8_T)IN_LRI_AS;
        }
      }
      break;

     case IN_LRI_AS:
      /* During 'LRI_AS': '<S1>:45' */
      if ((_sfEvent_ == event_v_p) || (_sfEvent_ == event_v_s)) {
        /* Transition: '<S1>:49' */
        /* Exit 'LRI_AS': '<S1>:45' */
        rtDWork.n_t_n = 0;

        /* Entry 'LRI': '<S1>:46' */
        rtDWork.bitsForTID0.is_PLRI = (uint8_T)IN_LRI;
        rtDWork.temporalCounter_i2 = 0UL;
      }
      break;

     default:
      rtDWork.bitsForTID0.is_PLRI = (uint8_T)IN_NO_ACTIVE_CHILD;
      break;
    }
  }

  /* During 'PPVARP': '<S1>:54' */
  if (rtDWork.bitsForTID0.is_active_PPVARP != 0) {
    switch (rtDWork.bitsForTID0.is_PPVARP) {
     case IN_ARPst1:
      /* During 'ARPst1': '<S1>:58' */
      if ((_sfEvent_ == event_v_p) || (_sfEvent_ == event_v_s)) {
        /* Transition: '<S1>:61' */
        /* Exit 'ARPst1': '<S1>:58' */
        rtDWork.n_t_b = 0;

        /* Entry 'ARPst2': '<S1>:56' */
        rtDWork.bitsForTID0.is_PPVARP = (uint8_T)IN_ARPst2;
        rtDWork.temporalCounter_i3 = 0UL;
      } else {
        if (_sfEvent_ == event_Ain) {
          /* Transition: '<S1>:64' */
          /* Exit 'ARPst1': '<S1>:58' */
          rtDWork.comm = rtDWork.comm + 1U;
          rtDWork.sh_rst = 1U;

          /* Entry 'inter_CC': '<S1>:57' */
          rtDWork.bitsForTID0.is_PPVARP = (uint8_T)IN_inter_CC;
        }
      }
      break;

     case IN_ARPst2:
      /* During 'ARPst2': '<S1>:56' */
      if ((rtDWork.sent == 0) && (rtDWork.comm == 0U) &&
          (rtDWork.temporalCounter_i3 == (uint32_T)(rtDWork.n_t_b + rtDWork.ARPd)))
      {
        /* Transition: '<S1>:63' */
        /* Exit 'ARPst2': '<S1>:56' */
        /* Entry 'ARPst1': '<S1>:58' */
        rtDWork.bitsForTID0.is_PPVARP = (uint8_T)IN_ARPst1;
      }
      break;

     case IN_inter_CC:
      /* During 'inter_CC': '<S1>:57' */
      if (rtDWork.sent == 0) {
        /* Transition: '<S1>:60' */
        /* Exit 'inter_CC': '<S1>:57' */
        rtDWork.sent = 2;
        rtDWork.sh_rst = 1U;
        rtDWork.comm = (rtDWork.comm - 1U);

        /* Entry 'ARPst1': '<S1>:58' */
        rtDWork.bitsForTID0.is_PPVARP = (uint8_T)IN_ARPst1;
      }
      break;

     default:
      rtDWork.bitsForTID0.is_PPVARP = (uint8_T)IN_NO_ACTIVE_CHILD;
      break;
    }
  }

  /* During 'PVRP': '<S1>:65' */
  if (rtDWork.bitsForTID0.is_active_PVRP != 0) {
    switch (rtDWork.bitsForTID0.is_PVRP) {
     case IN_VRPst1:
      /* During 'VRPst1': '<S1>:69' */
      if (_sfEvent_ == event_Vin) {
        /* Transition: '<S1>:72' */
        /* Exit 'VRPst1': '<S1>:69' */
        rtDWork.n_t_a1 = 0;
        rtDWork.comm = rtDWork.comm + 1U;
        rtDWork.sh_rst = 1U;

        /* Entry 'inter_CC': '<S1>:67' */
        rtDWork.bitsForTID0.is_PVRP = (uint8_T)IN_inter_CC;
      } else {
        if (_sfEvent_ == event_v_p) {
          /* Transition: '<S1>:74' */
          /* Exit 'VRPst1': '<S1>:69' */
          rtDWork.n_t_a1 = 0;

          /* Entry 'VRPst2': '<S1>:68' */
          rtDWork.bitsForTID0.is_PVRP = (uint8_T)IN_VRPst2;
          rtDWork.temporalCounter_i4 = 0UL;
        }
      }
      break;

     case IN_VRPst2:
      /* During 'VRPst2': '<S1>:68' */
      if ((rtDWork.sent == 0) && (rtDWork.comm == 0U) &&
          (rtDWork.temporalCounter_i4 == (uint32_T)(rtDWork.n_t_a1 +
            rtDWork.VRPd))) {
        /* Transition: '<S1>:73' */
        /* Exit 'VRPst2': '<S1>:68' */
        /* Entry 'VRPst1': '<S1>:69' */
        rtDWork.bitsForTID0.is_PVRP = (uint8_T)IN_VRPst1;
      }
      break;

     case IN_inter_CC:
      /* During 'inter_CC': '<S1>:67' */
      if (rtDWork.sent == 0) {
        /* Transition: '<S1>:71' */
        /* Exit 'inter_CC': '<S1>:67' */
        rtDWork.sent = 4;
        rtDWork.n_t_a1 = 0;
        rtDWork.sh_rst = 1U;
        rtDWork.comm = (rtDWork.comm - 1U);

        /* Entry 'VRPst2': '<S1>:68' */
        rtDWork.bitsForTID0.is_PVRP = (uint8_T)IN_VRPst2;
        rtDWork.temporalCounter_i4 = 0UL;
      }
      break;

     default:
      rtDWork.bitsForTID0.is_PVRP = (uint8_T)IN_NO_ACTIVE_CHILD;
      break;
    }
  }

  /* During 'PURI': '<S1>:75' */
  if (rtDWork.bitsForTID0.is_active_PURI != 0) {
    switch (rtDWork.bitsForTID0.is_PURI) {
     case IN_URIst1:
      /* During 'URIst1': '<S1>:78' */
      if ((_sfEvent_ == event_v_p) || (_sfEvent_ == event_v_s)) {
        /* Transition: '<S1>:82' */
        /* Exit 'URIst1': '<S1>:78' */
        rtDWork.n_t_a = 0;

        /* Entry 'URIst1': '<S1>:78' */
        rtDWork.bitsForTID0.is_PURI = (uint8_T)IN_URIst1;
        rtDWork.temporalCounter_i5 = 0UL;
      } else {
        if ((rtDWork.sent == 0) && (rtDWork.comm == 0U) &&
            (rtDWork.temporalCounter_i5 == (uint32_T)(rtDWork.n_t_a +
              rtDWork.URId))) {
          /* Transition: '<S1>:80' */
          /* Exit 'URIst1': '<S1>:78' */
          rtDWork.sent = 9;
          rtDWork.sh_rst = 1U;
          rtDWork.bitsForTID0.URIex = true;

          /* Entry 'URIst2': '<S1>:77' */
          rtDWork.bitsForTID0.is_PURI = (uint8_T)IN_URIst2;
        }
      }
      break;

     case IN_URIst2:
      /* During 'URIst2': '<S1>:77' */
      if ((_sfEvent_ == event_v_p) || (_sfEvent_ == event_v_s)) {
        /* Transition: '<S1>:83' */
        /* Exit 'URIst2': '<S1>:77' */
        rtDWork.n_t_a = 0;
        rtDWork.sh_rst = 1U;
        rtDWork.bitsForTID0.URIex = false;

        /* Entry 'URIst1': '<S1>:78' */
        rtDWork.bitsForTID0.is_PURI = (uint8_T)IN_URIst1;
        rtDWork.temporalCounter_i5 = 0UL;
      }
      break;

     default:
      rtDWork.bitsForTID0.is_PURI = (uint8_T)IN_NO_ACTIVE_CHILD;
      break;
    }
  }

  /* During 'Eng': '<S1>:143' */
  if ((rtDWork.bitsForTID0.is_active_Eng != 0) && (rtDWork.bitsForTID0.is_Eng ==
       IN_l0)) {
    /* During 'l0': '<S1>:142' */
    if (rtDWork.sent == 4) {
      /* Transition: '<S1>:151' */
      rtDWork.sent = -1;
      Send_VS1();

      /* Event: '<S1>:18' */
      sf_previousEvent = _sfEvent_;
      _sfEvent_ = event_v_s;
      c2_CodeExt_DDD_PMbuffer_rev3();
      _sfEvent_ = sf_previousEvent;
      if (rtDWork.bitsForTID0.is_Eng == IN_l0) {
        /* Exit 'l0': '<S1>:142' */
        /* Entry 'l0': '<S1>:142' */
        rtDWork.bitsForTID0.is_Eng = (uint8_T)IN_l0;
      }
    } else if (rtDWork.sent == 9) {
      /* Transition: '<S1>:145' */
      rtDWork.sent = -1;

      /* Event: '<S1>:23' */
      sf_previousEvent = _sfEvent_;
      _sfEvent_ = event_uri_s;
      c2_CodeExt_DDD_PMbuffer_rev3();
      _sfEvent_ = sf_previousEvent;
      if (rtDWork.bitsForTID0.is_Eng == IN_l0) {
        /* Exit 'l0': '<S1>:142' */
        /* Entry 'l0': '<S1>:142' */
        rtDWork.bitsForTID0.is_Eng = (uint8_T)IN_l0;
      }
    } else if (rtDWork.sent == -1) {
      /* Transition: '<S1>:146' */
      rtDWork.sent = 0;
      broadcast_tt();
      if (rtDWork.bitsForTID0.is_Eng == IN_l0) {
        /* Exit 'l0': '<S1>:142' */
        /* Entry 'l0': '<S1>:142' */
        rtDWork.bitsForTID0.is_Eng = (uint8_T)IN_l0;
      }
    } else if (rtDWork.sent == 1) {
      /* Transition: '<S1>:150' */
      rtDWork.sent = -1;
      Send_AP1();

      /* Event: '<S1>:15' */
      sf_previousEvent = _sfEvent_;
      _sfEvent_ = event_a_p;
      c2_CodeExt_DDD_PMbuffer_rev3();
      _sfEvent_ = sf_previousEvent;
      if (rtDWork.bitsForTID0.is_Eng == IN_l0) {
        /* Exit 'l0': '<S1>:142' */
        /* Entry 'l0': '<S1>:142' */
        rtDWork.bitsForTID0.is_Eng = (uint8_T)IN_l0;
      }
    } else if (rtDWork.sent == 2) {
      /* Transition: '<S1>:153' */
      rtDWork.sent = -1;
      Send_AS1();

      /* Event: '<S1>:16' */
      sf_previousEvent = _sfEvent_;
      _sfEvent_ = event_a_s;
      c2_CodeExt_DDD_PMbuffer_rev3();
      _sfEvent_ = sf_previousEvent;
      if (rtDWork.bitsForTID0.is_Eng == IN_l0) {
        /* Exit 'l0': '<S1>:142' */
        /* Entry 'l0': '<S1>:142' */
        rtDWork.bitsForTID0.is_Eng = (uint8_T)IN_l0;
      }
    } else if (rtDWork.sent == 3) {
      /* Transition: '<S1>:152' */
      rtDWork.sent = -1;
      Send_VP1();

      /* Event: '<S1>:17' */
      sf_previousEvent = _sfEvent_;
      _sfEvent_ = event_v_p;
      c2_CodeExt_DDD_PMbuffer_rev3();
      _sfEvent_ = sf_previousEvent;
      if (rtDWork.bitsForTID0.is_Eng == IN_l0) {
        /* Exit 'l0': '<S1>:142' */
        /* Entry 'l0': '<S1>:142' */
        rtDWork.bitsForTID0.is_Eng = (uint8_T)IN_l0;
      }
    } else {
      if (rtDWork.sh_rst == 1U) {
        /* Transition: '<S1>:144' */
        rtDWork.sh_rst = 0U;
        broadcast_tt();
        if (rtDWork.bitsForTID0.is_Eng == IN_l0) {
          /* Exit 'l0': '<S1>:142' */
          /* Entry 'l0': '<S1>:142' */
          rtDWork.bitsForTID0.is_Eng = (uint8_T)IN_l0;
        }
      }
    }
  }
}

static void broadcast_tt(void)
{
  int16_T sf_previousEvent;

  /* Event: '<S1>:27' */
  sf_previousEvent = _sfEvent_;
  _sfEvent_ = event_tt;
  c2_CodeExt_DDD_PMbuffer_rev3();
  _sfEvent_ = sf_previousEvent;
}

/* Initial conditions for trigger system: '<Root>/Chart1' */
void Chart1_Init(void)
{
  /* Initialize code for chart: '<Root>/Chart1' */
  rtDWork.bitsForTID0.is_active_Eng = 0U;
  rtDWork.bitsForTID0.is_Eng = 0U;
  rtDWork.bitsForTID0.is_PLRI = 0U;
  rtDWork.bitsForTID0.is_active_PPVARP = 0U;
  rtDWork.bitsForTID0.is_PPVARP = 0U;
  rtDWork.bitsForTID0.is_active_PURI = 0U;
  rtDWork.bitsForTID0.is_PURI = 0U;
  rtDWork.bitsForTID0.is_active_PVRP = 0U;
  rtDWork.bitsForTID0.is_PVRP = 0U;
  rtDWork.bitsForTID0.URIex = false;
  rtDWork.AVId = 150;
  rtDWork.LRId = 1000;
  rtDWork.ARPd = 200;
  rtDWork.VRPd = 150;
  rtDWork.URId = 400;
  rtDWork.sent = 0;
  rtDWork.comm = 0U;
  rtDWork.sh_rst = 0U;
  rtDWork.n_t_a = 0;
  rtDWork.n_t_b = 0;
  rtDWork.n_t_a1 = 0;
  rtDWork.APEventCounter = 0UL;
  AP = false;
  rtDWork.ASEventCounter = 0UL;
  AS = false;
  rtDWork.VPEventCounter = 0UL;
  VP = false;
  rtDWork.VSEventCounter = 0UL;
  VS = false;
  _sfEvent_ = CALL_EVENT;

  /* Entry: Chart1 */
  /* Entry 'PAVI': '<S1>:29' */
  rtDWork.n_t = 0;
  rtDWork.bitsForTID0.is_active_PAVI = 1U;

  /* Transition: '<S1>:35' */
  /* Entry 'st1': '<S1>:34' */
  rtDWork.bitsForTID0.is_PAVI = (uint8_T)IN_st1;

  /* Entry 'PLRI': '<S1>:43' */
  rtDWork.n_t_n = 0;
  rtDWork.bitsForTID0.is_active_PLRI = 1U;

  /* Transition: '<S1>:47' */
  /* Entry 'LRI': '<S1>:46' */
  if (rtDWork.bitsForTID0.is_PLRI != IN_LRI) {
    rtDWork.bitsForTID0.is_PLRI = (uint8_T)IN_LRI;
    rtDWork.temporalCounter_i2 = 0UL;
  }

  /* Entry 'PPVARP': '<S1>:54' */
  rtDWork.n_t_b = 0;
  rtDWork.bitsForTID0.is_active_PPVARP = 1U;

  /* Transition: '<S1>:59' */
  /* Entry 'ARPst1': '<S1>:58' */
  rtDWork.bitsForTID0.is_PPVARP = (uint8_T)IN_ARPst1;

  /* Entry 'PVRP': '<S1>:65' */
  rtDWork.n_t_a1 = 0;
  rtDWork.bitsForTID0.is_active_PVRP = 1U;

  /* Transition: '<S1>:70' */
  /* Entry 'VRPst1': '<S1>:69' */
  rtDWork.bitsForTID0.is_PVRP = (uint8_T)IN_VRPst1;

  /* Entry 'PURI': '<S1>:75' */
  rtDWork.n_t_a = 0;
  rtDWork.bitsForTID0.is_active_PURI = 1U;

  /* Transition: '<S1>:79' */
  /* Entry 'URIst1': '<S1>:78' */
  if (rtDWork.bitsForTID0.is_PURI != IN_URIst1) {
    rtDWork.bitsForTID0.is_PURI = (uint8_T)IN_URIst1;
    rtDWork.temporalCounter_i5 = 0UL;
  }

  /* Entry 'Eng': '<S1>:143' */
  rtDWork.bitsForTID0.is_active_Eng = 1U;

  /* Transition: '<S1>:149' */
  /* Entry 'l0': '<S1>:142' */
  rtDWork.bitsForTID0.is_Eng = (uint8_T)IN_l0;
}

/* Output and update for trigger system: '<Root>/Chart1' */
void Chart1(void)
{
  /* local block i/o variables */
  int8_T rtb_inputevents[3];

  {
    boolean_T zcEvent_idx;
    boolean_T zcEvent_idx_0;
    boolean_T zcEvent_idx_1;
    zcEvent_idx = (((rtPrevZCSigState.Chart1_Trig_ZCE[0] == POS_ZCSIG) != Vin) &&
                   (rtPrevZCSigState.Chart1_Trig_ZCE[0] != UNINITIALIZED_ZCSIG));
    zcEvent_idx_0 = (((rtPrevZCSigState.Chart1_Trig_ZCE[1] == POS_ZCSIG) != Ain)
                     && (rtPrevZCSigState.Chart1_Trig_ZCE[1] !=
                         UNINITIALIZED_ZCSIG));
    zcEvent_idx_1 = (((rtPrevZCSigState.Chart1_Trig_ZCE[2] == POS_ZCSIG) !=
                      clk_in) && (rtPrevZCSigState.Chart1_Trig_ZCE[2] !=
      UNINITIALIZED_ZCSIG));
    if (zcEvent_idx || zcEvent_idx_0 || zcEvent_idx_1) {
      rtb_inputevents[0] = (int8_T)(int16_T)(zcEvent_idx ? Vin ? RISING_ZCEVENT :
        FALLING_ZCEVENT : NO_ZCEVENT);
      rtb_inputevents[1] = (int8_T)(int16_T)(zcEvent_idx_0 ? Ain ?
        RISING_ZCEVENT : FALLING_ZCEVENT : NO_ZCEVENT);
      rtb_inputevents[2] = (int8_T)(int16_T)(zcEvent_idx_1 ? clk_in ?
        RISING_ZCEVENT : FALLING_ZCEVENT : NO_ZCEVENT);

      /* Stateflow: '<Root>/Chart1' */
      {
        int16_T sf_inputEventFiredFlag;
        int16_T sf_previousEvent;

        /* Gateway: Chart1 */
        sf_inputEventFiredFlag = 0;
        if (rtb_inputevents[0] != 0) {
          sf_inputEventFiredFlag = 1;

          /* Event: '<S1>:19' */
          sf_previousEvent = _sfEvent_;
          _sfEvent_ = event_Vin;
          c2_CodeExt_DDD_PMbuffer_rev3();
          _sfEvent_ = sf_previousEvent;
        }

        if (rtb_inputevents[1] != 0) {
          sf_inputEventFiredFlag = 1;

          /* Event: '<S1>:20' */
          sf_previousEvent = _sfEvent_;
          _sfEvent_ = event_Ain;
          c2_CodeExt_DDD_PMbuffer_rev3();
          _sfEvent_ = sf_previousEvent;
        }

        if (rtb_inputevents[2] != 0) {
          sf_inputEventFiredFlag = 1;

          /* Event: '<S1>:28' */
          sf_previousEvent = _sfEvent_;
          _sfEvent_ = event_clk;
          c2_CodeExt_DDD_PMbuffer_rev3();
          _sfEvent_ = sf_previousEvent;
        }

        if ((sf_inputEventFiredFlag != 0) && (rtDWork.APEventCounter > 0UL)) {
          AP = !AP;
          rtDWork.APEventCounter = (rtDWork.APEventCounter - 1UL);
        }

        if ((sf_inputEventFiredFlag != 0) && (rtDWork.ASEventCounter > 0UL)) {
          AS = !AS;
          rtDWork.ASEventCounter = (rtDWork.ASEventCounter - 1UL);
        }

        if ((sf_inputEventFiredFlag != 0) && (rtDWork.VPEventCounter > 0UL)) {
          VP = !VP;
          rtDWork.VPEventCounter = (rtDWork.VPEventCounter - 1UL);
        }

        if ((sf_inputEventFiredFlag != 0) && (rtDWork.VSEventCounter > 0UL)) {
          VS = !VS;
          rtDWork.VSEventCounter = (rtDWork.VSEventCounter - 1UL);
        }
      }
    }

    rtPrevZCSigState.Chart1_Trig_ZCE[0] = Vin ? POS_ZCSIG : ZERO_ZCSIG;
    rtPrevZCSigState.Chart1_Trig_ZCE[1] = Ain ? POS_ZCSIG : ZERO_ZCSIG;
    rtPrevZCSigState.Chart1_Trig_ZCE[2] = clk_in ? POS_ZCSIG : ZERO_ZCSIG;
  }
}

/* Model step function */
void CodeExt_DDD_PMbuffer_rev3_step(void)
{
  /* Stateflow: '<Root>/Chart1' incorporates:
   *  TriggerPort: '<S1>/ input events '
   */
  Chart1();
}

/* Model initialize function */
void CodeExt_DDD_PMbuffer_rev3_initialize(void)
{
  rtPrevZCSigState.Chart1_Trig_ZCE[0] = UNINITIALIZED_ZCSIG;
  rtPrevZCSigState.Chart1_Trig_ZCE[1] = UNINITIALIZED_ZCSIG;
  rtPrevZCSigState.Chart1_Trig_ZCE[2] = UNINITIALIZED_ZCSIG;

  /* Machine initializer */
  _sfEvent_ = CALL_EVENT;

  /* InitializeConditions for Stateflow: '<Root>/Chart1' */
  Chart1_Init();
}

/*
 * File trailer for Real-Time Workshop generated code.
 *
 * [EOF]
 */
