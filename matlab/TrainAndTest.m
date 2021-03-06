function score = TrainAndTest(label, features, selection, fold, classifier, eval)
% Training and testing with selected features
% Input:
%   label: m x 1 label vector (m is the number of samples)
%   features: m x s feature matrix (s is the number of different types of features)
%   selection: 1 x N binary vector (N is the number of features): 1 means corresponding
%              feature is selected; 0 means corresonding feature is not selected
%   fold: Integer n, which means n-fold cross-validation
%   classifier: "DT" - decision tree; "NB" - naive bayesian
%   eval: Evaluation: "F1", "accuracy" or "precision"
% Output:
%   score: Average evaluation value

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
    
    score = 0;
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
        if (strcmp(classifier, 'DT') == 1)
            DT = fitctree(feature_train, label_train);
            est = predict(DT, feature_test);
        else
            NB = fitNaiveBayes(feature_train, label_train);
            est = predict(NB, feature_test);
        end
        if (strcmp(eval, 'accuracy') == 1)
            accuracy = length(find(xor(est, label_test) == 0)) / length(label_test);
            score = score + accuracy;
        elseif (strcmp(eval, 'F1') == 1)
            if length(find(est)) == 0
                precision = 0;
            else
                precision = length(find(est & label_test)) / length(find(est));
            end
            recall = length(find(est & label_test)) / length(find(label_test));
            if precision + recall == 0
                F1 = 0;
            else
                F1 = 2 * precision * recall / (precision + recall);
            end
            score = score + F1;
        else
            if length(find(est)) == 0
                precision = 0;
            else
                precision = length(find(est & label_test)) / length(find(est));
            end
            score = score + precision;
        end
    end
    score = score / fold;
end