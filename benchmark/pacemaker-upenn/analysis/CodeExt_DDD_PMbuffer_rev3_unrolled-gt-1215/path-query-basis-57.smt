(benchmark gametime
:status unknown
:logic QF_AUFBV
:extrafuns ((gtINDEX23  BitVec[32]))
:extrafuns ((gtFIELD__bitsForTID0__gtAGG____anonstruct_D_Work_4  Array[32:32]))
:extrafuns ((gtINDEX24  BitVec[32]))
:extrafuns ((gtAGG__rtDWork  BitVec[32]))
:extrafuns ((gtINDEX22  BitVec[32]))
:extrafuns ((gtINDEX20  BitVec[32]))
:extrafuns ((gtINDEX21  BitVec[32]))
:extrafuns ((gtINDEX18  BitVec[32]))
:extrafuns ((gtINDEX19  BitVec[32]))
:extrafuns ((gtINDEX17  BitVec[32]))
:extrafuns ((gtINDEX16  BitVec[32]))
:extrafuns ((gtINDEX14  BitVec[32]))
:extrafuns ((gtINDEX15  BitVec[32]))
:extrafuns ((gtINDEX12  BitVec[32]))
:extrafuns ((gtINDEX13  BitVec[32]))
:extrafuns ((gtINDEX10  BitVec[32]))
:extrafuns ((gtINDEX11  BitVec[32]))
:extrafuns ((gtINDEX8  BitVec[32]))
:extrafuns ((gtINDEX9  BitVec[32]))
:extrafuns ((gtINDEX6  BitVec[32]))
:extrafuns ((gtINDEX7  BitVec[32]))
:extrafuns ((gtINDEX4  BitVec[32]))
:extrafuns ((gtINDEX5  BitVec[32]))
:extrafuns ((gtINDEX2  BitVec[32]))
:extrafuns ((gtINDEX3  BitVec[32]))
:extrafuns ((gtINDEX0  BitVec[32]))
:extrafuns ((gtINDEX1  BitVec[32]))
:extrafuns ((gtFIELD__is_active_Eng__gtAGG____anonstruct_bitsForTID0_5  Array[32:1]))
:extrafuns ((gtFIELD__temporalCounter_i5__gtAGG____anonstruct_D_Work_4  Array[32:32]))
:extrafuns ((gtFIELD__temporalCounter_i5__gtAGG____anonstruct_D_Work_4_1_  Array[32:32]))
:extrafuns ((gtFIELD__is_PURI__gtAGG____anonstruct_bitsForTID0_5  Array[32:2]))
:extrafuns ((gtFIELD__is_PURI__gtAGG____anonstruct_bitsForTID0_5_1_  Array[32:2]))
:extrafuns ((gtFIELD__URIex__gtAGG____anonstruct_bitsForTID0_5  Array[32:1]))
:extrafuns ((gtFIELD__URIex__gtAGG____anonstruct_bitsForTID0_5_1_  Array[32:1]))
:extrafuns ((gtFIELD__sh_rst__gtAGG____anonstruct_D_Work_4  Array[32:32]))
:extrafuns ((gtFIELD__sh_rst__gtAGG____anonstruct_D_Work_4_1_  Array[32:32]))
:extrafuns ((gtFIELD__n_t_a__gtAGG____anonstruct_D_Work_4  Array[32:32]))
:extrafuns ((gtFIELD__n_t_a__gtAGG____anonstruct_D_Work_4_1_  Array[32:32]))
:extrafuns ((sfEvent_  BitVec[32]))
:extrafuns ((gtFIELD__is_active_PURI__gtAGG____anonstruct_bitsForTID0_5  Array[32:1]))
:extrafuns ((gtFIELD__is_active_PVRP__gtAGG____anonstruct_bitsForTID0_5  Array[32:1]))
:extrafuns ((gtFIELD__is_active_PPVARP__gtAGG____anonstruct_bitsForTID0_5  Array[32:1]))
:extrafuns ((gtFIELD__is_active_PLRI__gtAGG____anonstruct_bitsForTID0_5  Array[32:1]))
:extrafuns ((gtFIELD__is_PAVI__gtAGG____anonstruct_bitsForTID0_5  Array[32:3]))
:extrafuns ((gtFIELD__is_active_PAVI__gtAGG____anonstruct_bitsForTID0_5  Array[32:1]))
:extrapreds ((constraint42 ))
:extrapreds ((constraint41 ))
:extrapreds ((constraint40 ))
:extrapreds ((constraint39 ))
:extrapreds ((constraint38 ))
:extrapreds ((constraint37 ))
:extrapreds ((constraint36 ))
:extrapreds ((constraint35 ))
:extrapreds ((constraint34 ))
:extrapreds ((constraint33 ))
:extrapreds ((constraint32 ))
:extrapreds ((constraint31 ))
:extrapreds ((constraint30 ))
:extrapreds ((constraint29 ))
:extrapreds ((constraint28 ))
:extrapreds ((constraint27 ))
:extrapreds ((constraint26 ))
:extrapreds ((constraint25 ))
:extrapreds ((constraint24 ))
:extrapreds ((constraint23 ))
:extrapreds ((constraint22 ))
:extrapreds ((constraint21 ))
:extrapreds ((constraint20 ))
:extrapreds ((constraint19 ))
:extrapreds ((constraint18 ))
:extrapreds ((constraint17 ))
:extrapreds ((constraint16 ))
:extrapreds ((constraint15 ))
:extrapreds ((constraint14 ))
:extrapreds ((constraint13 ))
:extrapreds ((constraint12 ))
:extrapreds ((constraint11 ))
:extrapreds ((constraint10 ))
:extrapreds ((constraint9 ))
:extrapreds ((constraint8 ))
:extrapreds ((constraint7 ))
:extrapreds ((constraint6 ))
:extrapreds ((constraint5 ))
:extrapreds ((constraint4 ))
:extrapreds ((constraint3 ))
:extrapreds ((constraint2 ))
:extrapreds ((constraint1 ))
:extrapreds ((constraint0 ))
:formula
(flet ($x253 (and constraint0 constraint1 constraint2 constraint3 constraint4 constraint5 constraint6 constraint7 constraint8 constraint9 constraint10 constraint11 constraint12 constraint13 constraint14 constraint15 constraint16 constraint17 constraint18 constraint19 constraint20 constraint21 constraint22 constraint23 constraint24 constraint25 constraint26 constraint27 constraint28 constraint29 constraint30 constraint31 constraint32 constraint33 constraint34 constraint35 constraint36 constraint37 constraint38 constraint39 constraint40 constraint41 constraint42))
(flet ($x165 (= gtINDEX24 (select gtFIELD__bitsForTID0__gtAGG____anonstruct_D_Work_4 gtINDEX23)))
(flet ($x160 (= gtINDEX21 (select gtFIELD__bitsForTID0__gtAGG____anonstruct_D_Work_4 gtINDEX20)))
(flet ($x156 (= gtINDEX19 (select gtFIELD__bitsForTID0__gtAGG____anonstruct_D_Work_4 gtINDEX18)))
(flet ($x150 (= gtINDEX15 (select gtFIELD__bitsForTID0__gtAGG____anonstruct_D_Work_4 gtINDEX14)))
(flet ($x146 (= gtINDEX13 (select gtFIELD__bitsForTID0__gtAGG____anonstruct_D_Work_4 gtINDEX12)))
(flet ($x142 (= gtINDEX11 (select gtFIELD__bitsForTID0__gtAGG____anonstruct_D_Work_4 gtINDEX10)))
(flet ($x138 (= gtINDEX9 (select gtFIELD__bitsForTID0__gtAGG____anonstruct_D_Work_4 gtINDEX8)))
(flet ($x134 (= gtINDEX7 (select gtFIELD__bitsForTID0__gtAGG____anonstruct_D_Work_4 gtINDEX6)))
(flet ($x130 (= gtINDEX5 (select gtFIELD__bitsForTID0__gtAGG____anonstruct_D_Work_4 gtINDEX4)))
(flet ($x126 (= gtINDEX3 (select gtFIELD__bitsForTID0__gtAGG____anonstruct_D_Work_4 gtINDEX2)))
(flet ($x122 (= gtINDEX1 (select gtFIELD__bitsForTID0__gtAGG____anonstruct_D_Work_4 gtINDEX0)))
(let (?x113 (select gtFIELD__is_active_Eng__gtAGG____anonstruct_bitsForTID0_5 gtINDEX24))
(let (?x110 (store gtFIELD__temporalCounter_i5__gtAGG____anonstruct_D_Work_4 gtINDEX22 bv0[32]))
(flet ($x199 (iff constraint16 (= gtFIELD__temporalCounter_i5__gtAGG____anonstruct_D_Work_4_1_ ?x110)))
(let (?x107 (store gtFIELD__is_PURI__gtAGG____anonstruct_bitsForTID0_5 gtINDEX21 (extract[1:0] bv1[32])))
(flet ($x197 (iff constraint15 (= gtFIELD__is_PURI__gtAGG____anonstruct_bitsForTID0_5_1_ ?x107)))
(let (?x103 (store gtFIELD__URIex__gtAGG____anonstruct_bitsForTID0_5 gtINDEX19 (extract[0:0] bv0[32])))
(flet ($x195 (iff constraint14 (= gtFIELD__URIex__gtAGG____anonstruct_bitsForTID0_5_1_ ?x103)))
(flet ($x100 (= gtFIELD__sh_rst__gtAGG____anonstruct_D_Work_4_1_ (store gtFIELD__sh_rst__gtAGG____anonstruct_D_Work_4 gtINDEX17 bv1[32])))
(flet ($x97 (= gtFIELD__n_t_a__gtAGG____anonstruct_D_Work_4_1_ (store gtFIELD__n_t_a__gtAGG____anonstruct_D_Work_4 gtINDEX16 bv0[32])))
(let (?x90 (select gtFIELD__is_PURI__gtAGG____anonstruct_bitsForTID0_5 gtINDEX15))
(let (?x85 (select gtFIELD__is_PURI__gtAGG____anonstruct_bitsForTID0_5 gtINDEX13))
(let (?x80 (select gtFIELD__is_active_PURI__gtAGG____anonstruct_bitsForTID0_5 gtINDEX11))
(let (?x74 (select gtFIELD__is_active_PVRP__gtAGG____anonstruct_bitsForTID0_5 gtINDEX9))
(let (?x68 (select gtFIELD__is_active_PPVARP__gtAGG____anonstruct_bitsForTID0_5 gtINDEX7))
(let (?x62 (select gtFIELD__is_active_PLRI__gtAGG____anonstruct_bitsForTID0_5 gtINDEX5))
(let (?x52 (zero_extend[29] (select gtFIELD__is_PAVI__gtAGG____anonstruct_bitsForTID0_5 gtINDEX3)))
(let (?x45 (select gtFIELD__is_active_PAVI__gtAGG____anonstruct_bitsForTID0_5 gtINDEX1))
(flet ($x252 (and (iff constraint0 (not (= sfEvent_ bv8[32]))) (iff constraint1 (not (= (zero_extend[31] ?x45) bv0[32]))) (iff constraint2 (= ?x52 bv1[32])) (iff constraint3 (not (= sfEvent_ bv2[32]))) (iff constraint4 (not (= sfEvent_ bv3[32]))) (iff constraint5 (not (not (= (zero_extend[31] ?x62) bv0[32])))) (iff constraint6 (not (not (= (zero_extend[31] ?x68) bv0[32])))) (iff constraint7 (not (not (= (zero_extend[31] ?x74) bv0[32])))) (iff constraint8 (not (= (zero_extend[31] ?x80) bv0[32]))) (iff constraint9 (not (= (zero_extend[30] ?x85) bv1[32]))) (iff constraint10 (= (zero_extend[30] ?x90) bv2[32])) (iff constraint11 (= sfEvent_ bv4[32])) (iff constraint12 $x97) (iff constraint13 $x100) $x195 $x197 $x199 (iff constraint17 (not (not (= (zero_extend[31] ?x113) bv0[32])))) (iff constraint18 (= gtINDEX0 gtAGG__rtDWork)) (iff constraint19 $x122) (iff constraint20 (= gtINDEX2 gtAGG__rtDWork)) (iff constraint21 $x126) (iff constraint22 (= gtINDEX4 gtAGG__rtDWork)) (iff constraint23 $x130) (iff constraint24 (= gtINDEX6 gtAGG__rtDWork)) (iff constraint25 $x134) (iff constraint26 (= gtINDEX8 gtAGG__rtDWork)) (iff constraint27 $x138) (iff constraint28 (= gtINDEX10 gtAGG__rtDWork)) (iff constraint29 $x142) (iff constraint30 (= gtINDEX12 gtAGG__rtDWork)) (iff constraint31 $x146) (iff constraint32 (= gtINDEX14 gtAGG__rtDWork)) (iff constraint33 $x150) (iff constraint34 (= gtINDEX16 gtAGG__rtDWork)) (iff constraint35 (= gtINDEX17 gtAGG__rtDWork)) (iff constraint36 (= gtINDEX18 gtAGG__rtDWork)) (iff constraint37 $x156) (iff constraint38 (= gtINDEX20 gtAGG__rtDWork)) (iff constraint39 $x160) (iff constraint40 (= gtINDEX22 gtAGG__rtDWork)) (iff constraint41 (= gtINDEX23 gtAGG__rtDWork)) (iff constraint42 $x165)))
(and $x252 $x253)))))))))))))))))))))))))))))))
)
