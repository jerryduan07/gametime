/*
 * File: CodeExt_DDD_PMbuffer_rev3.h
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

#ifndef RTW_HEADER_CodeExt_DDD_PMbuffer_rev3_h_
#define RTW_HEADER_CodeExt_DDD_PMbuffer_rev3_h_
#ifndef CodeExt_DDD_PMbuffer_rev3_COMMON_INCLUDES_
# define CodeExt_DDD_PMbuffer_rev3_COMMON_INCLUDES_
#include "rtwtypes.h"
#endif                                 /* CodeExt_DDD_PMbuffer_rev3_COMMON_INCLUDES_ */

#include "CodeExt_DDD_PMbuffer_rev3_types.h"

/* Macros for accessing real-time model data structure */
#ifndef rtmGetErrorStatus
# define rtmGetErrorStatus(rtm)        ((rtm)->errorStatus)
#endif

#ifndef rtmSetErrorStatus
# define rtmSetErrorStatus(rtm, val)   ((rtm)->errorStatus = (val))
#endif

/* user code (top of header file) */
#include "SendHW.h"

/* Block states (auto storage) for system '<Root>' */
typedef struct {
  uint32_T APEventCounter;             /* '<Root>/Chart1' */
  uint32_T ASEventCounter;             /* '<Root>/Chart1' */
  uint32_T VPEventCounter;             /* '<Root>/Chart1' */
  uint32_T VSEventCounter;             /* '<Root>/Chart1' */
  uint32_T temporalCounter_i1;         /* '<Root>/Chart1' */
  uint32_T temporalCounter_i2;         /* '<Root>/Chart1' */
  uint32_T temporalCounter_i3;         /* '<Root>/Chart1' */
  uint32_T temporalCounter_i4;         /* '<Root>/Chart1' */
  uint32_T temporalCounter_i5;         /* '<Root>/Chart1' */
  int16_T AVId;                        /* '<Root>/Chart1' */
  int16_T LRId;                        /* '<Root>/Chart1' */
  int16_T ARPd;                        /* '<Root>/Chart1' */
  int16_T VRPd;                        /* '<Root>/Chart1' */
  int16_T URId;                        /* '<Root>/Chart1' */
  int16_T sent;                        /* '<Root>/Chart1' */
  int16_T n_t;                         /* '<Root>/Chart1' */
  int16_T n_t_n;                       /* '<Root>/Chart1' */
  int16_T n_t_a;                       /* '<Root>/Chart1' */
  int16_T n_t_b;                       /* '<Root>/Chart1' */
  int16_T n_t_a1;                      /* '<Root>/Chart1' */
  uint16_T comm;                       /* '<Root>/Chart1' */
  uint16_T sh_rst;                     /* '<Root>/Chart1' */
  struct {
    uint_T is_PAVI:3;                  /* '<Root>/Chart1' */
    uint_T is_PLRI:2;                  /* '<Root>/Chart1' */
    uint_T is_PPVARP:2;                /* '<Root>/Chart1' */
    uint_T is_PVRP:2;                  /* '<Root>/Chart1' */
    uint_T is_PURI:2;                  /* '<Root>/Chart1' */
    uint_T is_active_PAVI:1;           /* '<Root>/Chart1' */
    uint_T is_active_PLRI:1;           /* '<Root>/Chart1' */
    uint_T is_active_PPVARP:1;         /* '<Root>/Chart1' */
    uint_T is_active_PVRP:1;           /* '<Root>/Chart1' */
    uint_T is_active_PURI:1;           /* '<Root>/Chart1' */
    uint_T is_active_Eng:1;            /* '<Root>/Chart1' */
    uint_T is_Eng:1;                   /* '<Root>/Chart1' */
    uint_T URIex:1;                    /* '<Root>/Chart1' */
  } bitsForTID0;
} D_Work;

/* Zero-crossing (trigger) state */
typedef struct {
  ZCSigState Chart1_Trig_ZCE[3];       /* '<Root>/Chart1' */
} PrevZCSigStates;

/* Real-time Model Data Structure */
struct RT_MODEL {
  const char_T * volatile errorStatus;
};

/* Block states (auto storage) */
extern D_Work rtDWork;

/*
 * Exported Global Signals
 *
 * Note: Exported global signals are block signals with an exported global
 * storage class designation.  RTW declares the memory for these signals
 * and exports their symbols.
 *
 */
extern boolean_T Vin;                  /* '<Root>/In1' */
extern boolean_T Ain;                  /* '<Root>/In2' */
extern boolean_T clk_in;               /* '<Root>/In3' */
extern boolean_T AP;                   /* '<Root>/Chart1' */
extern boolean_T AS;                   /* '<Root>/Chart1' */
extern boolean_T VP;                   /* '<Root>/Chart1' */
extern boolean_T VS;                   /* '<Root>/Chart1' */

/* Model entry point functions */
extern void CodeExt_DDD_PMbuffer_rev3_initialize(void);
extern void CodeExt_DDD_PMbuffer_rev3_step(void);

/* Real-time Model object */
extern RT_MODEL *rtM;

/*-
 * The generated code includes comments that allow you to trace directly
 * back to the appropriate location in the model.  The basic format
 * is <system>/block_name, where system is the system number (uniquely
 * assigned by Simulink) and block_name is the name of the block.
 *
 * Use the MATLAB hilite_system command to trace the generated code back
 * to the model.  For example,
 *
 * hilite_system('<S3>')    - opens system 3
 * hilite_system('<S3>/Kp') - opens and selects block Kp which resides in S3
 *
 * Here is the system hierarchy for this model
 *
 * '<Root>' : CodeExt_DDD_PMbuffer_rev3
 * '<S1>'   : CodeExt_DDD_PMbuffer_rev3/Chart1
 */
#endif                                 /* RTW_HEADER_CodeExt_DDD_PMbuffer_rev3_h_ */

/*
 * File trailer for Real-Time Workshop generated code.
 *
 * [EOF]
 */
