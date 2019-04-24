\* gt-FindExtremePath *\
Maximize
OBJ: EdgeFlow_2
Subject To
Flows_from_source: EdgeFlow_2 = 1
Flows_through_10: EdgeFlow_0 - EdgeFlow_12 = 0
Flows_through_11: EdgeFlow_1 - EdgeFlow_11 = 0
Flows_through_12: - EdgeFlow_15 + EdgeFlow_2 = 0
Flows_through_13: - EdgeFlow_13 - EdgeFlow_14 + EdgeFlow_4 = 0
Flows_through_14: - EdgeFlow_18 + EdgeFlow_3 = 0
Flows_through_15: - EdgeFlow_16 - EdgeFlow_17 + EdgeFlow_6 = 0
Flows_through_16: - EdgeFlow_20 + EdgeFlow_8 = 0
Flows_through_17: - EdgeFlow_19 + EdgeFlow_7 = 0
Flows_through_18: EdgeFlow_10 - EdgeFlow_22 = 0
Flows_through_19: - EdgeFlow_21 + EdgeFlow_5 = 0
Flows_through_2: EdgeFlow_15 - EdgeFlow_4 = 0
Flows_through_20: - EdgeFlow_0 + EdgeFlow_9 = 0
Flows_through_21: - EdgeFlow_1 + EdgeFlow_12 = 0
Flows_through_3: EdgeFlow_13 - EdgeFlow_3 = 0
Flows_through_4: EdgeFlow_14 - EdgeFlow_6 = 0
Flows_through_5: EdgeFlow_18 + EdgeFlow_22 - EdgeFlow_5 = 0
Flows_through_6: EdgeFlow_17 - EdgeFlow_8 = 0
Flows_through_7: EdgeFlow_16 - EdgeFlow_7 = 0
Flows_through_8: - EdgeFlow_10 + EdgeFlow_19 + EdgeFlow_20 = 0
Flows_through_9: EdgeFlow_21 - EdgeFlow_9 = 0
Flows_to_sink: EdgeFlow_11 = 1
Binaries
EdgeFlow_0
EdgeFlow_1
EdgeFlow_10
EdgeFlow_11
EdgeFlow_12
EdgeFlow_13
EdgeFlow_14
EdgeFlow_15
EdgeFlow_16
EdgeFlow_17
EdgeFlow_18
EdgeFlow_19
EdgeFlow_2
EdgeFlow_20
EdgeFlow_21
EdgeFlow_22
EdgeFlow_3
EdgeFlow_4
EdgeFlow_5
EdgeFlow_6
EdgeFlow_7
EdgeFlow_8
EdgeFlow_9
End
