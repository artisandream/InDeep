//Maya ASCII 2016 scene
//Name: Level1.ma
//Last modified: Wed, Dec 23, 2015 12:42:01 PM
//Codeset: UTF-8
requires maya "2016";
currentUnit -l centimeter -a degree -t film;
fileInfo "application" "maya";
fileInfo "product" "Maya 2016";
fileInfo "version" "2016";
fileInfo "cutIdentifier" "201510022200-973226";
fileInfo "osv" "Mac OS X 10.9.1";
fileInfo "license" "student";
createNode transform -s -n "persp";
	rename -uid "1D59EC71-534F-9635-2627-0C9C4040C9E7";
	setAttr ".v" no;
	setAttr ".t" -type "double3" 158.40186307576116 125.76214922550588 160.62915594019213 ;
	setAttr ".r" -type "double3" -29.138352729603454 44.600000000001089 -2.2334538879381066e-15 ;
createNode camera -s -n "perspShape" -p "persp";
	rename -uid "428A05AE-7A45-43C2-9ED5-B6AC5BC02D8A";
	setAttr -k off ".v" no;
	setAttr ".fl" 34.999999999999986;
	setAttr ".coi" 258.28084354389603;
	setAttr ".imn" -type "string" "persp";
	setAttr ".den" -type "string" "persp_depth";
	setAttr ".man" -type "string" "persp_mask";
	setAttr ".hc" -type "string" "viewSet -p %camera";
createNode transform -s -n "top";
	rename -uid "A770CCF4-A749-15E7-4E0A-A5A74E3A8648";
	setAttr ".v" no;
	setAttr ".t" -type "double3" 0 100.1 0 ;
	setAttr ".r" -type "double3" -89.999999999999986 0 0 ;
createNode camera -s -n "topShape" -p "top";
	rename -uid "2DBD5E3F-EA4B-0EF0-1A3F-D588F7B06B72";
	setAttr -k off ".v" no;
	setAttr ".rnd" no;
	setAttr ".coi" 100.1;
	setAttr ".ow" 30;
	setAttr ".imn" -type "string" "top";
	setAttr ".den" -type "string" "top_depth";
	setAttr ".man" -type "string" "top_mask";
	setAttr ".hc" -type "string" "viewSet -t %camera";
	setAttr ".o" yes;
createNode transform -s -n "front";
	rename -uid "48905B05-774E-1C93-CFB1-92A508716DAB";
	setAttr ".v" no;
	setAttr ".t" -type "double3" 0 0 100.1 ;
createNode camera -s -n "frontShape" -p "front";
	rename -uid "41D7D7E7-9B4C-8B40-56B7-0A81CB4F61FA";
	setAttr -k off ".v" no;
	setAttr ".rnd" no;
	setAttr ".coi" 100.1;
	setAttr ".ow" 30;
	setAttr ".imn" -type "string" "front";
	setAttr ".den" -type "string" "front_depth";
	setAttr ".man" -type "string" "front_mask";
	setAttr ".hc" -type "string" "viewSet -f %camera";
	setAttr ".o" yes;
createNode transform -s -n "side";
	rename -uid "C98F045D-5548-686C-1026-CAAADA89AA4D";
	setAttr ".v" no;
	setAttr ".t" -type "double3" 100.1 0 0 ;
	setAttr ".r" -type "double3" 0 89.999999999999986 0 ;
createNode camera -s -n "sideShape" -p "side";
	rename -uid "164DB575-2345-ABB5-4ADC-71A1F5E07F93";
	setAttr -k off ".v" no;
	setAttr ".rnd" no;
	setAttr ".coi" 100.1;
	setAttr ".ow" 30;
	setAttr ".imn" -type "string" "side";
	setAttr ".den" -type "string" "side_depth";
	setAttr ".man" -type "string" "side_mask";
	setAttr ".hc" -type "string" "viewSet -s %camera";
	setAttr ".o" yes;
createNode transform -n "IllustratorCurves1";
	rename -uid "07D4D9C6-404A-FA95-CCFD-119E8C2F6CFA";
	setAttr ".r" -type "double3" 90 0 0 ;
createNode transform -n "CompoundCurve1" -p "IllustratorCurves1";
	rename -uid "6CD5B560-3F4B-43D6-80ED-A483682996D4";
createNode transform -n "curve1" -p "CompoundCurve1";
	rename -uid "E72ED6D6-8649-25AE-9DCE-99AFDAD46377";
	setAttr ".t" -type "double3" -67.5 75 3.1554436208840472e-30 ;
	setAttr ".rp" -type "double3" 73.06027777777777 -72.822152777777774 0 ;
	setAttr ".sp" -type "double3" 73.06027777777777 -72.822152777777774 0 ;
createNode nurbsCurve -n "curveShape1" -p "curve1";
	rename -uid "EE485788-444F-DD57-B908-23ACD63760FE";
	setAttr -k off ".v";
	setAttr ".cc" -type "nurbsCurve" 
		3 10 1 no 3
		15 0 0 0 1 1 1 2 2 2 3 3 3 4 4 4
		13
		142.29291666666666 -72.822152777777774 0
		142.29291666666666 -111.05828266666664 0
		111.29640766666665 -142.05479166666666 0
		73.06027777777777 -142.05479166666666 0
		34.824147888888888 -142.05479166666666 0
		3.827638888888889 -111.05828266666664 0
		3.827638888888889 -72.822152777777774 0
		3.827638888888889 -34.586022888888884 0
		34.824147888888888 -3.5895138888888889 0
		73.06027777777777 -3.5895138888888889 0
		111.29640766666665 -3.5895138888888889 0
		142.29291666666666 -34.586022888888884 0
		142.29291666666666 -72.822152777777774 0
		;
createNode transform -n "CompoundCurve2" -p "IllustratorCurves1";
	rename -uid "346A6024-5B4C-ED42-3D7A-ADB829B5A7E3";
createNode transform -n "curve2" -p "CompoundCurve2";
	rename -uid "F411C3A3-FE4F-EE1C-2E07-2C88B5E4863D";
	setAttr ".t" -type "double3" -67.5 75 3.1554436208840472e-30 ;
	setAttr ".rp" -type "double3" 80.742013888888891 -49.968767361111105 0 ;
	setAttr ".sp" -type "double3" 80.742013888888891 -49.968767361111105 0 ;
createNode nurbsCurve -n "curveShape2" -p "curve2";
	rename -uid "27EFDA8E-D34D-89CC-D390-99B6AB271C94";
	setAttr -k off ".v";
	setAttr ".cc" -type "nurbsCurve" 
		3 4 0 no 3
		9 0 0 0 1 1 1 2 2 2
		7
		103.45208333333333 -46.337361111111107 0
		103.45208333333333 -56.086357361111105 0
		97.253188638888886 -71.252291666666665 0
		84.710763888888891 -71.252291666666665 0
		72.168339138888896 -71.252291666666665 0
		58.031944444444441 -56.086357361111105 0
		58.031944444444441 -46.337361111111107 0
		;
createNode transform -n "CompoundCurve3" -p "IllustratorCurves1";
	rename -uid "AEFC3988-AF47-C751-7A36-9BAC97C1F7C0";
createNode transform -n "curve3" -p "CompoundCurve3";
	rename -uid "4EBA509D-0641-5593-6FB7-06BF79938C68";
	setAttr ".t" -type "double3" -67.5 75 3.1554436208840472e-30 ;
	setAttr ".rp" -type "double3" 54.945138888888891 -103.88423611111111 0 ;
	setAttr ".sp" -type "double3" 54.945138888888891 -103.88423611111111 0 ;
createNode nurbsCurve -n "curveShape3" -p "curve3";
	rename -uid "93492FB3-6F4F-34FF-2DB0-E7B6BE940275";
	setAttr -k off ".v";
	setAttr ".cc" -type "nurbsCurve" 
		3 4 0 no 3
		9 0 0 0 1 1 1 2 2 2
		7
		80.742013888887556 -99.033541666665315 0.99998000039998924
		80.742013888887556 -108.78253791666333 0.99997888931233159
		64.400758083333315 -121.53635416666665 0
		51.858333333332993 -121.53635416666665 -1.9999800001999977
		39.315908583333332 -121.53635416666665 0
		29.148263888888888 -113.63323236111111 0
		29.148263888888724 -103.88423611111109 0
		;
createNode transform -n "CompoundCurve4" -p "IllustratorCurves1";
	rename -uid "4D16E379-E74E-D831-085D-11B805674F61";
createNode transform -n "curve4" -p "CompoundCurve4";
	rename -uid "C5EDB7B2-CD4C-0A0F-1BCE-F991175DB12E";
	setAttr ".t" -type "double3" -67.5 75 3.1554436208840472e-30 ;
	setAttr ".rp" -type "double3" 49.538819444444442 -57.58876736111111 0 ;
	setAttr ".sp" -type "double3" 49.538819444444442 -57.58876736111111 0 ;
createNode nurbsCurve -n "curveShape4" -p "curve4";
	rename -uid "B8E7892E-D14F-4539-2906-85A92AF902C9";
	setAttr -k off ".v";
	setAttr ".cc" -type "nurbsCurve" 
		3 4 0 no 3
		9 0 0 0 1 1 1 2 2 2
		7
		72.248888888888516 -59.787013888888509 -1.4999700006055421
		72.248888888887777 -69.536010138887789 -1.499968333974059
		62.081244194444437 -77.439131944444441 0
		49.538819444444442 -77.439131944444071 0
		36.996394694444447 -77.439131944444441 0
		26.828749999999999 -69.536010138888884 0
		32.524805983592969 -56.465282886520413 7.3757244808812344e-16
		;
createNode transform -n "curve3detachedCurve2";
	rename -uid "7FD4985F-C440-9A39-E8B1-3CB35136EDF2";
	setAttr ".t" -type "double3" -67.5 1.6653345369377345e-14 75 ;
	setAttr ".r" -type "double3" 89.999999999999986 0 0 ;
createNode nurbsCurve -n "curve3detachedCurveShape2" -p "curve3detachedCurve2";
	rename -uid "C55B083F-E34C-F825-0F5C-4EA7796405BB";
	setAttr -k off ".v";
	setAttr ".cc" -type "nurbsCurve" 
		3 4 0 no 3
		9 2 2 2 3 3 3 4 4 4
		7
		29.148263888888724 -103.88423611111109 0
		29.148263888888888 -94.135239861111103 0
		39.315908583333332 -86.232118055555574 0
		55.531551612620113 -82.240824076938225 1.5096957902697635e-14
		64.400758083333315 -86.232118055555574 0
		80.742013888887556 -89.284545416667243 0.99998111148764646
		80.742013888887556 -99.033541666665315 0.99998000039998924
		;
createNode transform -n "curve4detachedCurve2";
	rename -uid "C5503CB6-264C-947E-304A-2F96B04EAEFA";
	setAttr ".t" -type "double3" -67.5 1.6653345369377345e-14 75 ;
	setAttr ".r" -type "double3" 89.999999999999986 0 0 ;
createNode nurbsCurve -n "curve4detachedCurveShape2" -p "curve4detachedCurve2";
	rename -uid "BACC3BDD-CC49-7192-E5A1-2792FCC97045";
	setAttr -k off ".v";
	setAttr ".cc" -type "nurbsCurve" 
		3 4 0 no 3
		9 2 2 2 3 3 3 4 4 4
		7
		32.524805983592969 -56.465282886520413 7.3757244808812344e-16
		26.828749999999999 -50.038017638888881 0
		34.465214138888889 -37.738402777777765 0
		47.0076388888887 -37.738402777777587 0
		59.550063638888879 -37.738402777777765 0
		72.248888888889212 -50.038017638889187 -1.4999705561493699
		72.248888888888516 -59.787013888888509 -1.4999700006055421
		;
createNode transform -n "curve2detachedCurve2";
	rename -uid "7E77FF1D-B246-E825-20BE-7A90C5426052";
	setAttr ".t" -type "double3" -67.5 1.6653345369377345e-14 75 ;
	setAttr ".r" -type "double3" 89.999999999999986 0 0 ;
createNode nurbsCurve -n "curve2detachedCurveShape2" -p "curve2detachedCurve2";
	rename -uid "6A0C63C4-E34B-CF75-053E-FB84559BA583";
	setAttr -k off ".v";
	setAttr ".cc" -type "nurbsCurve" 
		3 4 0 no 3
		9 2 2 2 3 3 3 4 4 4
		7
		58.031944444444441 -46.337361111111107 0
		58.031944444444441 -36.58836486111111 0
		68.199589138888896 -28.685243055555553 0
		80.742013888888891 -28.685243055555553 0
		93.284438638888886 -28.685243055555553 0
		103.45208333333333 -36.58836486111111 0
		103.45208333333333 -46.337361111111107 0
		;
createNode transform -n "offsetNurbsCurve1";
	rename -uid "A74969C6-314D-CAD9-2651-A58B33DD9875";
createNode nurbsCurve -n "offsetNurbsCurveShape1" -p "offsetNurbsCurve1";
	rename -uid "D33F4128-2D4F-B527-7C70-B8B1A76E3683";
	setAttr -k off ".v";
	setAttr ".tw" yes;
createNode transform -n "offsetNurbsCurve2";
	rename -uid "93B588FC-1B41-6827-FD7E-3B9D6EF0DFB9";
createNode nurbsCurve -n "offsetNurbsCurveShape2" -p "offsetNurbsCurve2";
	rename -uid "CB10B46C-B144-86D1-3D81-DDAE87FE46FD";
	setAttr -k off ".v";
	setAttr ".tw" yes;
createNode transform -n "offsetNurbsCurve3";
	rename -uid "6BF9C452-8F46-7727-DD71-879CA2460B80";
createNode nurbsCurve -n "offsetNurbsCurveShape3" -p "offsetNurbsCurve3";
	rename -uid "B90784EB-4A4A-8C70-89CE-048556228C53";
	setAttr -k off ".v";
	setAttr ".tw" yes;
createNode transform -n "offsetNurbsCurve4";
	rename -uid "72461FEE-2040-FFD7-FF6D-3F840544F2BA";
createNode nurbsCurve -n "offsetNurbsCurveShape4" -p "offsetNurbsCurve4";
	rename -uid "7DA2F3CD-EC4B-0849-2FB4-71B82132136A";
	setAttr -k off ".v";
	setAttr ".tw" yes;
createNode transform -n "offsetNurbsCurve5";
	rename -uid "E0B56784-DA40-C11F-225F-7E97D11E7D77";
createNode nurbsCurve -n "offsetNurbsCurveShape5" -p "offsetNurbsCurve5";
	rename -uid "C019B4ED-1E4A-12E8-A0FA-85B3CD4CCCEC";
	setAttr -k off ".v";
	setAttr ".tw" yes;
createNode transform -n "offsetNurbsCurve6";
	rename -uid "6B0A2CD2-0F48-375A-2438-ACAB869D375A";
createNode nurbsCurve -n "offsetNurbsCurveShape6" -p "offsetNurbsCurve6";
	rename -uid "90E35617-804C-FFB9-F58E-93B96A01CF52";
	setAttr -k off ".v";
	setAttr ".tw" yes;
createNode transform -n "loftedSurface1";
	rename -uid "B3D00DAD-B64B-A61A-2DBC-DD9D9C35C9D3";
createNode mesh -n "loftedSurfaceShape1" -p "loftedSurface1";
	rename -uid "C108076E-3C4B-4B59-C034-2282D2F4B215";
	setAttr -k off ".v";
	setAttr ".vir" yes;
	setAttr ".vif" yes;
	setAttr ".uvst[0].uvsn" -type "string" "map1";
	setAttr ".cuvs" -type "string" "map1";
	setAttr ".dcc" -type "string" "Ambient+Diffuse";
	setAttr ".covm[0]"  0 1 1;
	setAttr ".cdvm[0]"  0 1 1;
createNode transform -n "loftedSurface2";
	rename -uid "8DD3AE2D-DA4B-722F-8FBF-32867056509D";
createNode mesh -n "loftedSurfaceShape2" -p "loftedSurface2";
	rename -uid "52461970-0941-091D-EC0A-6592DC4DB6F3";
	setAttr -k off ".v";
	setAttr ".vir" yes;
	setAttr ".vif" yes;
	setAttr ".uvst[0].uvsn" -type "string" "map1";
	setAttr ".cuvs" -type "string" "map1";
	setAttr ".dcc" -type "string" "Ambient+Diffuse";
	setAttr ".covm[0]"  0 1 1;
	setAttr ".cdvm[0]"  0 1 1;
createNode transform -n "loftedSurface3";
	rename -uid "ACBFD8E3-9C40-43BD-D9F9-33A9BCD84EE3";
createNode mesh -n "loftedSurfaceShape3" -p "loftedSurface3";
	rename -uid "A0E25780-D44F-46D8-E1C8-808749C72111";
	setAttr -k off ".v";
	setAttr ".vir" yes;
	setAttr ".vif" yes;
	setAttr ".uvst[0].uvsn" -type "string" "map1";
	setAttr ".cuvs" -type "string" "map1";
	setAttr ".dcc" -type "string" "Ambient+Diffuse";
	setAttr ".covm[0]"  0 1 1;
	setAttr ".cdvm[0]"  0 1 1;
createNode transform -n "loftedSurface4";
	rename -uid "27023091-504B-8C3F-B788-FD9C8679D59D";
createNode mesh -n "loftedSurfaceShape4" -p "loftedSurface4";
	rename -uid "3CF11006-B244-4D99-70E7-EABFCBDA62C5";
	setAttr -k off ".v";
	setAttr ".vir" yes;
	setAttr ".vif" yes;
	setAttr ".uvst[0].uvsn" -type "string" "map1";
	setAttr ".cuvs" -type "string" "map1";
	setAttr ".dcc" -type "string" "Ambient+Diffuse";
	setAttr ".covm[0]"  0 1 1;
	setAttr ".cdvm[0]"  0 1 1;
createNode transform -n "loftedSurface5";
	rename -uid "F3E84A29-1746-9A41-53F9-128F640C90AA";
createNode mesh -n "loftedSurfaceShape5" -p "loftedSurface5";
	rename -uid "AA33A72C-DF43-29AA-9B0E-A2A4DFB49E4E";
	setAttr -k off ".v";
	setAttr ".vir" yes;
	setAttr ".vif" yes;
	setAttr ".uvst[0].uvsn" -type "string" "map1";
	setAttr ".cuvs" -type "string" "map1";
	setAttr ".dcc" -type "string" "Ambient+Diffuse";
	setAttr ".covm[0]"  0 1 1;
	setAttr ".cdvm[0]"  0 1 1;
createNode transform -n "loftedSurface6";
	rename -uid "A53A6BDE-1442-F7B6-97E8-00AD9FB54E3F";
createNode mesh -n "loftedSurfaceShape6" -p "loftedSurface6";
	rename -uid "C071C47B-7D47-F48D-7FE1-1CAD80064865";
	setAttr -k off ".v";
	setAttr ".vir" yes;
	setAttr ".vif" yes;
	setAttr ".uvst[0].uvsn" -type "string" "map1";
	setAttr ".cuvs" -type "string" "map1";
	setAttr ".dcc" -type "string" "Ambient+Diffuse";
	setAttr ".covm[0]"  0 1 1;
	setAttr ".cdvm[0]"  0 1 1;
createNode lightLinker -s -n "lightLinker1";
	rename -uid "25954A55-AE43-783A-C3C6-2D8A9EF04456";
	setAttr -s 2 ".lnk";
	setAttr -s 2 ".slnk";
createNode displayLayerManager -n "layerManager";
	rename -uid "3D55A8B3-E34C-8FAD-5F48-71849F669534";
createNode displayLayer -n "defaultLayer";
	rename -uid "29F7396F-104C-D749-7CC1-6A942D9286CF";
createNode renderLayerManager -n "renderLayerManager";
	rename -uid "E5242E1E-F74D-FB5C-C3DA-EDAFB6FE64B6";
createNode renderLayer -n "defaultRenderLayer";
	rename -uid "D69EE640-2C40-3FBC-D263-A2B389D21F7F";
	setAttr ".g" yes;
createNode makeIllustratorCurves -n "makeIllustratorCurves1";
	rename -uid "972DAB2C-C04D-BACE-741D-EA8BF74A00E3";
	setAttr ".ifn" -type "string" "/Users/anthonyromrell/Documents/GitRepos/Chalk/Chalk/Assets/PathBuilding/IllustratorFiles/Level_001.ai";
	setAttr -s 4 ".p";
createNode offsetCurve -n "offsetCurve1";
	rename -uid "2B1130F4-E544-ED7D-4530-85B15E2B220A";
	setAttr ".cl" yes;
	setAttr ".d" -3;
	setAttr ".ugn" no;
createNode offsetCurve -n "offsetCurve2";
	rename -uid "2A26E824-AF4E-54D0-E153-B78B4C6EED59";
	setAttr ".cl" yes;
	setAttr ".d" -3;
	setAttr ".ugn" no;
createNode offsetCurve -n "offsetCurve3";
	rename -uid "D422C66E-A64E-A73A-D661-468B7972FE7D";
	setAttr ".cl" yes;
	setAttr ".d" -3;
	setAttr ".ugn" no;
createNode offsetCurve -n "offsetCurve4";
	rename -uid "6DE23DF9-624D-DE44-F422-DB85D3B55331";
	setAttr ".cl" yes;
	setAttr ".d" -3;
	setAttr ".ugn" no;
createNode offsetCurve -n "offsetCurve5";
	rename -uid "64A8BE60-0946-C22F-114A-5F8EDE9CF564";
	setAttr ".cl" yes;
	setAttr ".d" -3;
	setAttr ".ugn" no;
createNode offsetCurve -n "offsetCurve6";
	rename -uid "7EB1E5FF-274D-07DE-26D5-E1A28A3B58E7";
	setAttr ".cl" yes;
	setAttr ".d" -3;
	setAttr ".ugn" no;
createNode loft -n "loft1";
	rename -uid "7C86E91A-274D-70E8-AB34-009F1B13331E";
	setAttr -s 2 ".ic";
	setAttr ".u" yes;
	setAttr ".rsn" yes;
createNode nurbsTessellate -n "nurbsTessellate1";
	rename -uid "05012DC8-9946-4046-60E5-5D8AF8277251";
	setAttr ".f" 2;
	setAttr ".pt" 1;
	setAttr ".pc" 500;
	setAttr ".chr" 0.1;
	setAttr ".un" 2;
	setAttr ".vn" 2;
	setAttr ".ucr" no;
	setAttr ".cht" 0.2;
createNode loft -n "loft2";
	rename -uid "5576F434-2142-2F60-2ED0-17B808922F33";
	setAttr -s 2 ".ic";
	setAttr ".u" yes;
	setAttr ".rsn" yes;
createNode nurbsTessellate -n "nurbsTessellate2";
	rename -uid "39392DA2-F344-2413-78D8-FCBB369FB9D1";
	setAttr ".f" 2;
	setAttr ".pt" 1;
	setAttr ".pc" 500;
	setAttr ".chr" 0.1;
	setAttr ".un" 2;
	setAttr ".vn" 2;
	setAttr ".ucr" no;
	setAttr ".cht" 0.2;
createNode loft -n "loft3";
	rename -uid "31FB71BC-0E44-8013-5F25-FCBE54B4C4D0";
	setAttr -s 2 ".ic";
	setAttr ".u" yes;
	setAttr ".rsn" yes;
createNode nurbsTessellate -n "nurbsTessellate3";
	rename -uid "B5DF367C-E14C-E8E0-F308-89A27E9E0DD6";
	setAttr ".f" 2;
	setAttr ".pt" 1;
	setAttr ".pc" 500;
	setAttr ".chr" 0.1;
	setAttr ".un" 2;
	setAttr ".vn" 2;
	setAttr ".ucr" no;
	setAttr ".cht" 0.2;
createNode loft -n "loft4";
	rename -uid "88A7711F-6645-EC08-5EB1-139B2BBBD6E8";
	setAttr -s 2 ".ic";
	setAttr ".u" yes;
	setAttr ".rsn" yes;
createNode nurbsTessellate -n "nurbsTessellate4";
	rename -uid "A617B97E-284B-6311-729A-78A6917D6355";
	setAttr ".f" 2;
	setAttr ".pt" 1;
	setAttr ".pc" 500;
	setAttr ".chr" 0.1;
	setAttr ".un" 2;
	setAttr ".vn" 2;
	setAttr ".ucr" no;
	setAttr ".cht" 0.2;
createNode loft -n "loft5";
	rename -uid "371015FD-FE4A-2F8A-36DB-51BCF2EFBD3C";
	setAttr -s 2 ".ic";
	setAttr ".u" yes;
	setAttr ".rsn" yes;
createNode nurbsTessellate -n "nurbsTessellate5";
	rename -uid "52E48616-3A4D-4416-FA95-8E8A6E3D1F84";
	setAttr ".f" 2;
	setAttr ".pt" 1;
	setAttr ".pc" 500;
	setAttr ".chr" 0.1;
	setAttr ".un" 2;
	setAttr ".vn" 2;
	setAttr ".ucr" no;
	setAttr ".cht" 0.2;
createNode loft -n "loft6";
	rename -uid "502B71B4-C64A-BF0C-F674-AA8EC8025E12";
	setAttr -s 2 ".ic";
	setAttr ".u" yes;
	setAttr ".rsn" yes;
createNode nurbsTessellate -n "nurbsTessellate6";
	rename -uid "5F836C06-EF49-B83E-77E5-BFBFD22E2181";
	setAttr ".f" 2;
	setAttr ".pt" 1;
	setAttr ".pc" 500;
	setAttr ".chr" 0.1;
	setAttr ".un" 2;
	setAttr ".vn" 2;
	setAttr ".ucr" no;
	setAttr ".cht" 0.2;
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
	setAttr -s 2 ".st";
select -ne :renderGlobalsList1;
select -ne :defaultShaderList1;
	setAttr -s 4 ".s";
select -ne :postProcessList1;
	setAttr -s 2 ".p";
select -ne :defaultRenderingList1;
select -ne :initialShadingGroup;
	setAttr -s 6 ".dsm";
	setAttr ".ro" yes;
select -ne :initialParticleSE;
	setAttr ".ro" yes;
select -ne :defaultRenderGlobals;
	setAttr ".ren" -type "string" "mayaHardware";
select -ne :defaultResolution;
	setAttr ".pa" 1;
select -ne :hardwareRenderGlobals;
	setAttr ".ctrs" 256;
	setAttr ".btrs" 512;
select -ne :ikSystem;
	setAttr -s 4 ".sol";
connectAttr "makeIllustratorCurves1.p[0]" "CompoundCurve1.t";
connectAttr "makeIllustratorCurves1.p[1]" "CompoundCurve2.t";
connectAttr "makeIllustratorCurves1.p[2]" "CompoundCurve3.t";
connectAttr "makeIllustratorCurves1.p[3]" "CompoundCurve4.t";
connectAttr "offsetCurve1.oc[0]" "offsetNurbsCurveShape1.cr";
connectAttr "offsetCurve2.oc[0]" "offsetNurbsCurveShape2.cr";
connectAttr "offsetCurve3.oc[0]" "offsetNurbsCurveShape3.cr";
connectAttr "offsetCurve4.oc[0]" "offsetNurbsCurveShape4.cr";
connectAttr "offsetCurve5.oc[0]" "offsetNurbsCurveShape5.cr";
connectAttr "offsetCurve6.oc[0]" "offsetNurbsCurveShape6.cr";
connectAttr "nurbsTessellate1.op" "loftedSurfaceShape1.i";
connectAttr "nurbsTessellate2.op" "loftedSurfaceShape2.i";
connectAttr "nurbsTessellate3.op" "loftedSurfaceShape3.i";
connectAttr "nurbsTessellate4.op" "loftedSurfaceShape4.i";
connectAttr "nurbsTessellate5.op" "loftedSurfaceShape5.i";
connectAttr "nurbsTessellate6.op" "loftedSurfaceShape6.i";
relationship "link" ":lightLinker1" ":initialShadingGroup.message" ":defaultLightSet.message";
relationship "link" ":lightLinker1" ":initialParticleSE.message" ":defaultLightSet.message";
relationship "shadowLink" ":lightLinker1" ":initialShadingGroup.message" ":defaultLightSet.message";
relationship "shadowLink" ":lightLinker1" ":initialParticleSE.message" ":defaultLightSet.message";
connectAttr "layerManager.dli[0]" "defaultLayer.id";
connectAttr "renderLayerManager.rlmi[0]" "defaultRenderLayer.rlid";
connectAttr "curveShape3.ws" "offsetCurve1.ic";
connectAttr "curve3detachedCurveShape2.ws" "offsetCurve2.ic";
connectAttr "curveShape4.ws" "offsetCurve3.ic";
connectAttr "curve4detachedCurveShape2.ws" "offsetCurve4.ic";
connectAttr "curve2detachedCurveShape2.ws" "offsetCurve5.ic";
connectAttr "curveShape2.ws" "offsetCurve6.ic";
connectAttr "curveShape2.ws" "loft1.ic[0]";
connectAttr "offsetNurbsCurveShape6.ws" "loft1.ic[1]";
connectAttr "loft1.os" "nurbsTessellate1.is";
connectAttr "curveShape3.ws" "loft2.ic[0]";
connectAttr "offsetNurbsCurveShape1.ws" "loft2.ic[1]";
connectAttr "loft2.os" "nurbsTessellate2.is";
connectAttr "curve3detachedCurveShape2.ws" "loft3.ic[0]";
connectAttr "offsetNurbsCurveShape2.ws" "loft3.ic[1]";
connectAttr "loft3.os" "nurbsTessellate3.is";
connectAttr "curveShape4.ws" "loft4.ic[0]";
connectAttr "offsetNurbsCurveShape3.ws" "loft4.ic[1]";
connectAttr "loft4.os" "nurbsTessellate4.is";
connectAttr "curve4detachedCurveShape2.ws" "loft5.ic[0]";
connectAttr "offsetNurbsCurveShape4.ws" "loft5.ic[1]";
connectAttr "loft5.os" "nurbsTessellate5.is";
connectAttr "curve2detachedCurveShape2.ws" "loft6.ic[0]";
connectAttr "offsetNurbsCurveShape5.ws" "loft6.ic[1]";
connectAttr "loft6.os" "nurbsTessellate6.is";
connectAttr "defaultRenderLayer.msg" ":defaultRenderingList1.r" -na;
connectAttr "loftedSurfaceShape1.iog" ":initialShadingGroup.dsm" -na;
connectAttr "loftedSurfaceShape2.iog" ":initialShadingGroup.dsm" -na;
connectAttr "loftedSurfaceShape3.iog" ":initialShadingGroup.dsm" -na;
connectAttr "loftedSurfaceShape4.iog" ":initialShadingGroup.dsm" -na;
connectAttr "loftedSurfaceShape5.iog" ":initialShadingGroup.dsm" -na;
connectAttr "loftedSurfaceShape6.iog" ":initialShadingGroup.dsm" -na;
// End of Level1.ma
