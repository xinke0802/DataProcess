function result = EvaluateVote(label, features, fold)
    N = length(features);
    m = length(label);
    interval = floor(m / fold);
    
    
    selections = cell(1, 12);
    for i = 1:1:12
        selections{i} = zeros(1, N);
    end
    selections{1}([12,14,20,27,31,32,33,34,39,40]) = 1;
    selections{2}([2,7,13,15,18,20,21,29,31,32,33,34,37,39,40]) = 1;
    selections{3}([5,6,8,9,12,13,16,17,19,25,26,27,28,29,33,34,35,38,41,43]) = 1;
    selections{4}([8,9,10,11,14,27,32,39,42,44]) = 1;
    selections{5}([5,6,12,22,23,24,28,31,34,43]) = 1;
    selections{6}([6,12,17,20,26,27,28,33,34,35,37,38,40,42]) = 1;
    selections{7}([2,3,12,13,14,15,22,27,31,33,34,35,36,37,39,40,41,42,44,45]) = 1;
    selections{8}([19,20,21,29,31,34,37,39,41,44]) = 1;
    selections{9}([5,6,13,16,17,19,25,26,27,28,29,33,34,41,43]) = 1;
    selections{10}([1,2,8,9,17,23,24,25,26,34,35,36,37,39,43]) = 1;
    selections{11}([1,6,8,9,10,11,14,16,17,18,20,21,23,24,25,26,27,30,32,34,35,38,43]) = 1;
    selections{12}([6,11,14,15,18,22,25,26,29,31,33,34,35,36,39,40,41]) = 1;
    
    feature = cell(1, 12);
    for i = 1:1:6
        for j = 1:1:N
            if selections{i}(j) == 0
                continue;
            end
            feature{i} = [feature{i}, features{j}];
        end
    end
    
    for i = 1:1:N
        features{i}(isnan(features{i})) = 0;
    end
    for i = 7:1:12
        for j = 1:1:N
            if selections{i}(j) == 0
                continue;
            end
            feature{i} = [feature{i}, features{j}];
        end
    end
    
    accuracy_All = [];
    F1_All = [];
    precision_All = [];
    accuracy_DT = [];
    F1_DT = [];
    precision_DT = [];
    accuracy_NB = [];
    F1_NB = [];
    precision_NB = [];
    for i = 1:1:fold
        lower = (i-1) * interval + 1;
        if i == fold
            upper = m;
        else
            upper = i * interval;
        end
        
        
        est_All = zeros(upper - lower + 1, 1);
        est_DT = zeros(upper - lower + 1, 1);
        for j = 1:1:6
            feature_test = feature{j}(lower:upper, :);
            label_test = label(lower:upper);
            removeIndex = true(1, size(feature{j}, 1));
            removeIndex(lower:upper) = false;
            feature_train = feature{j}(removeIndex, :);
            label_train = label(removeIndex);

            DT = fitctree(feature_train, label_train);
            est_DT = est_DT + predict(DT, feature_test);
            est_All = est_All + predict(DT, feature_test);
        end
        est_DT = est_DT >= 2;
        
        accuracy = length(find(xor(est_DT, label_test) == 0)) / length(label_test);
        if length(find(est_DT)) == 0
            precision = 0;
        else
            precision = length(find(est_DT & label_test)) / length(find(est_DT));
        end
        recall = length(find(est_DT & label_test)) / length(find(label_test));
        if precision + recall == 0
            F1 = 0;
        else
            F1 = 2 * precision * recall / (precision + recall);
        end
        accuracy_DT = [accuracy_DT, accuracy];
        F1_DT = [F1_DT, F1];
        precision_DT = [precision_DT, precision];
        
        
        est_NB = zeros(upper - lower + 1, 1);
        for j = 7:1:12
            feature_test = feature{j}(lower:upper, :);
            label_test = label(lower:upper);
            removeIndex = true(1, size(feature{j}, 1));
            removeIndex(lower:upper) = false;
            feature_train = feature{j}(removeIndex, :);
            label_train = label(removeIndex);

            NB = fitNaiveBayes(feature_train, label_train);
            est_NB = est_NB + predict(NB, feature_test);
            est_All = est_All + predict(NB, feature_test);
        end
        est_NB = est_NB >= 2;
        
        accuracy = length(find(xor(est_NB, label_test) == 0)) / length(label_test);
        if length(find(est_NB)) == 0
            precision = 0;
        else
            precision = length(find(est_NB & label_test)) / length(find(est_NB));
        end
        recall = length(find(est_NB & label_test)) / length(find(label_test));
        if precision + recall == 0
            F1 = 0;
        else
            F1 = 2 * precision * recall / (precision + recall);
        end
        accuracy_NB = [accuracy_NB, accuracy];
        F1_NB = [F1_NB, F1];
        precision_NB = [precision_NB, precision];
        
        
        est_All = est_All >= 4;
        
        accuracy = length(find(xor(est_All, label_test) == 0)) / length(label_test);
        if length(find(est_All)) == 0
            precision = 0;
        else
            precision = length(find(est_All & label_test)) / length(find(est_All));
        end
        recall = length(find(est_All & label_test)) / length(find(label_test));
        if precision + recall == 0
            F1 = 0;
        else
            F1 = 2 * precision * recall / (precision + recall);
        end
        accuracy_All = [accuracy_All, accuracy];
        F1_All = [F1_All, F1];
        precision_All = [precision_All, precision];
    end
    
    meanAcc_DT = mean(accuracy_DT);
    stdAcc_DT = std(accuracy_DT);
    meanF1_DT = mean(F1_DT);
    stdF1_DT = std(F1_DT);
    meanPrecision_DT = mean(precision_DT);
    stdPrecision_DT = std(precision_DT);
    meanAcc_NB = mean(accuracy_NB);
    stdAcc_NB = std(accuracy_NB);
    meanF1_NB = mean(F1_NB);
    stdF1_NB = std(F1_NB);
    meanPrecision_NB = mean(precision_NB);
    stdPrecision_NB = std(precision_NB);
    meanAcc_All = mean(accuracy_All);
    stdAcc_All = std(accuracy_All);
    meanF1_All = mean(F1_All);
    stdF1_All = std(F1_All);
    meanPrecision_All = mean(precision_All);
    stdPrecision_All = std(precision_All);
    
    result = [meanAcc_DT meanF1_DT meanPrecision_DT stdAcc_DT stdF1_DT stdPrecision_DT;
              meanAcc_NB meanF1_NB meanPrecision_NB stdAcc_NB stdF1_NB stdPrecision_NB;
              meanAcc_All meanF1_All meanPrecision_All stdAcc_All stdF1_All stdPrecision_All];
end