function score = TrainAndTest(label, features, selection, fold, classifier, eval)
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
            precision = length(find(est & label_test)) / length(find(est));
            recall = length(find(est & label_test)) / length(find(label_test));
            F1 = 2 * precision * recall / (precision + recall);
            score = score + F1;
        else
            precision = length(find(est & label_test)) / length(find(est));
            score = score + precision;
        end
    end
    score = score / fold;
end