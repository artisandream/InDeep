//Maya ASCII 2016 scene
//Name: useBackground.ma
//Last modified: Fri, Dec 25, 2015 12:59:39 AM
//Codeset: UTF-8
requires maya "2016";
currentUnit -l centimeter -a degree -t film;
fileInfo "application" "maya";
fileInfo "product" "Maya 2016";
fileInfo "version" "2016";
fileInfo "cutIdentifier" "201502261600-953408";
fileInfo "osv" "Mac OS X 10.9.5";
createNode transform -n "pCube1";
	rename -uid "430349D8-D949-34AA-E979-77814B800FC6";
createNode mesh -n "pCubeShape1" -p "pCube1";
	rename -uid "D129ECB6-714A-E767-A92D-7493838DC0FA";
	setAttr -k off ".v";
	setAttr ".vir" yes;
	setAttr ".vif" yes;
	setAttr ".uvst[0].uvsn" -type "string" "map1";
	setAttr ".cuvs" -type "string" "map1";
	setAttr ".dcc" -type "string" "Ambient+Diffuse";
	setAttr ".covm[0]"  0 1 1;
	setAttr ".cdvm[0]"  0 1 1;
createNode polyCube -n "polyCube1";
	rename -uid "0DCF6030-D74B-7FAB-1DFA-008097BB53FE";
	setAttr ".cuv" 4;
createNode materialInfo -n "materialInfo1";
	rename -uid "D2E0D1BB-6242-E65A-C5FC-1D9893D26F67";
createNode shadingEngine -n "useBackground1SG";
	rename -uid "D795F035-7748-95EE-E659-3087ADA77CF4";
	setAttr ".ihi" 0;
	setAttr ".ro" yes;
createNode useBackground -n "useBackground1";
	rename -uid "C7F38278-9C4A-C3BF-EAB9-D0819A6AF16B";
createNode lightLinker -s -n "lightLinker1";
	rename -uid "67164F56-394C-E92E-40D3-A9A7CC293D65";
	setAttr -s 3 ".lnk";
	setAttr -s 3 ".slnk";
select -ne :time1;
	setAttr ".o" 1;
	setAttr ".unw" 1;
select -ne :hardwareRenderingGlobals;
	setAttr ".otfna" -type "stringArray" 22 "NURBS Curves" "NURBS Surfaces" "Polygons" "Subdiv Surface" "Particles" "Particle Instance" "Fluids" "Strokes" "Image Planes" "UI" "Lights" "Cameras" "Locators" "Joints" "IK Handles" "Deformers" "Motion Trails" "Components" "Hair Systems" "Follicles" "Misc. UI" "Ornaments"  ;
	setAttr ".otfva" -type "Int32Array" 22 0 1 1 1 1 1
		 1 1 1 0 0 0 0 0 0 0 0 0
		 0 0 0 0 ;
	setAttr ".fprt" yes;
select -ne :renderPartition;
	setAttr -s 3 ".st";
select -ne :renderGlobalsList1;
select -ne :defaultShaderList1;
	setAttr -s 5 ".s";
select -ne :postProcessList1;
	setAttr -s 2 ".p";
select -ne :defaultRenderingList1;
select -ne :initialShadingGroup;
	setAttr ".ro" yes;
select -ne :initialParticleSE;
	setAttr ".ro" yes;
select -ne :defaultResolution;
	setAttr ".pa" 1;
select -ne :hardwareRenderGlobals;
	setAttr ".ctrs" 256;
	setAttr ".btrs" 512;
connectAttr "polyCube1.out" "pCubeShape1.i";
connectAttr "useBackground1SG.msg" "materialInfo1.sg";
connectAttr "useBackground1.msg" "materialInfo1.m";
connectAttr "useBackground1.msg" "materialInfo1.t" -na;
connectAttr "useBackground1.oc" "useBackground1SG.ss";
connectAttr "pCubeShape1.iog" "useBackground1SG.dsm" -na;
relationship "link" ":lightLinker1" ":initialShadingGroup.message" ":defaultLightSet.message";
relationship "link" ":lightLinker1" ":initialParticleSE.message" ":defaultLightSet.message";
relationship "link" ":lightLinker1" "useBackground1SG.message" ":defaultLightSet.message";
relationship "shadowLink" ":lightLinker1" ":initialShadingGroup.message" ":defaultLightSet.message";
relationship "shadowLink" ":lightLinker1" ":initialParticleSE.message" ":defaultLightSet.message";
relationship "shadowLink" ":lightLinker1" "useBackground1SG.message" ":defaultLightSet.message";
connectAttr "useBackground1SG.pa" ":renderPartition.st" -na;
connectAttr "useBackground1.msg" ":defaultShaderList1.s" -na;
// End of useBackground.ma
