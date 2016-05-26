function result = EvaluateSelection(label, features, selection, fold)
    N = length(features);
    m = length(label);
    interval = floor(m / fold);
    
    
    
    feature = [];
    for i = 1:1:N
        if selection(i) == 0
            continue;
        end
        feature = [feature, features{i}];
    end
    
    accuracy_DT = [];
    F1_DT = [];
    precision_DT = [];
    for i = 1:1:fold
        lower = (i-1) * interval + 1;
        if i == fold
            upper = m;
        else
            upper = i * interval;
        end
        feature_test = feature(lower:upper, :);
        label_test = label(lower:upper);
        removeIndex = true(1, size(feature, 1));
        removeIndex(lower:upper) = false;
        feature_train = feature(removeIndex, :);
        label_train = label(removeIndex);
        
        DT = fitctree(feature_train, label_train);
        est_DT = predict(DT, feature_test);
        
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
    end
    
    
    
    
    for i = 1:1:N
        features{i}(isnan(features{i})) = 0;
    end
    
    feature = [];
    for i = 1:1:N
        if selection(i) == 0
            continue;
        end
        feature = [feature, features{i}];
    end
    
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
        feature_test = feature(lower:upper, :);
        label_test = label(lower:upper);
        removeIndex = true(1, size(feature, 1));
        removeIndex(lower:upper) = false;
        feature_train = feature(removeIndex, :);
        label_train = label(removeIndex);
        
        NB = fitNaiveBayes(feature_train, label_train);
        est_NB = predict(NB, feature_test);
        
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
    
    result = [meanAcc_DT meanF1_DT meanPrecision_DT stdAcc_DT stdF1_DT stdPrecision_DT;
              meanAcc_NB meanF1_NB meanPrecision_NB stdAcc_NB stdF1_NB stdPrecision_NB];
end