function selection = Wrapper(label, features, classifier, eval, mode, initState)
% Wrapper feature selection
% Input:
%   label: m x 1 label vector (m is the number of samples)
%   features: m x s feature matrix (s is the number of different types of features)
%   classifier: "DT" - decision tree; "NB" - naive bayesian
%   eval: Evaluation or optimization objective in interation: "F1", "accuracy" or "precision"
%   mode: "forward"  - only add new features to initial selected feature set
%         "backward" - only delete features from initial selected feature set
%         "float"    - every interation will add a feature to or delete a feature from selected feature set
%   initState:  1 x N binary vector (N is the number of features): 1 means corresponding
%              feature is selected initially; 0 means corresonding feature is not selected initially
% Output:
%   selection: 1 x N binary vector (N is the number of features): 1 means corresponding
%              feature is selected; 0 means corresonding feature is not selected

    N = length(features);
    if strcmp(classifier, 'DT') == 0
        for i = 1:1:N
            features{i}(isnan(features{i})) = 0;
        end
    end
    
    if isequal(initState, zeros(1, N)) == 1
        openList = zeros(1, N);
        scoreList = zeros(1, 1);
    else
        openList = initState;
        score = TrainAndTest(label, features, initState, 5, classifier, eval);
        scoreList = score;
    end
    closedList = [];
    best = [];
    bestScore = 0;
    iterCount = 0;
    iterNoChange = 0;
    
    while iterNoChange < 500 && iterCount < 6000 && size(openList, 1) ~= 0
        maxIndex = find(scoreList == max(scoreList));
        candidateIndex = maxIndex(1);
        candidate = openList(candidateIndex, :);
        candidateScore = scoreList(candidateIndex);
        
        removeIndex = true(1, size(openList, 1));
        removeIndex(candidateIndex) = false;
        openList = openList(removeIndex, :);
        scoreList = scoreList(removeIndex);
        closedList = [closedList; candidate];
        
        if candidateScore - 1e-10 > bestScore && length(find(candidate)) >= 10 && length(find(candidate)) <= 35
            best = candidate;
            bestScore = candidateScore;
            iterNoChange = 0;
        elseif isequal(best, []) == 0 && length(find(candidate)) >= 10 && length(find(candidate)) <= 35
            iterNoChange = iterNoChange + 1
        end
        
        openList_new = [];
        scoreList_new = [];
        for i = 1:1:N
            if strcmp(mode, 'backward')
                if candidate(i) == 1
                    open_new = candidate;
                    open_new(i) = 0;
                    if isempty(find(open_new))
                        continue;
                    end
                else
                    continue;
                end
            elseif strcmp(mode, 'forward')
                if candidate(i) == 0
                    open_new = candidate;
                    open_new(i) = 1;
                else
                    continue;
                end
            else
                if candidate(i) == 1
                    open_new = candidate;
                    open_new(i) = 0;
                    if isempty(find(open_new))
                        continue;
                    end
                else
                    open_new = candidate;
                    open_new(i) = 1;
                end
            end
            if (isequal(openList, []) == 0 && ismember(open_new, openList, 'rows')) || (isequal(closedList, []) == 0 && ismember(open_new, closedList, 'rows'))
                continue;
            end
            score = TrainAndTest(label, features, open_new, 5, classifier, eval);
            openList_new = [openList_new; open_new];
            scoreList_new = [scoreList_new; score];
        end
        
        if isequal(openList_new, [])
            continue;
        end
        maxIndex = find(scoreList_new == max(scoreList_new));
        maxIndex = maxIndex(1);
        open_new = openList_new(maxIndex, :);
        maxScore = scoreList_new(maxIndex);
        openList = [openList; open_new];
        scoreList = [scoreList; maxScore];
        removeIndex = true(1, size(openList_new, 1));
        removeIndex(maxIndex) = false;
        openList_new = openList_new(removeIndex, :);
        scoreList_new = scoreList_new(removeIndex);
        
        while size(openList_new, 1) ~= 0
            maxIndex = find(scoreList_new == max(scoreList_new));
            maxIndex = maxIndex(1);
            open_new = open_new + openList_new(maxIndex, :) - candidate;
            
            if isempty(find(open_new))
                open_new = open_new - openList_new(maxIndex, :) + candidate;
            elseif (isequal(openList, []) == 0 && ismember(open_new, openList, 'rows')) || (isequal(closedList, []) == 0 && ismember(open_new, closedList, 'rows'))
                open_new = open_new - openList_new(maxIndex, :) + candidate;
            else
                score = TrainAndTest(label, features, open_new, 5, classifier, eval);
                if score - 1e-10 > maxScore
                    maxScore = score;
                    openList = [openList; open_new];
                    scoreList = [scoreList; score];
                else
                    break;
                end
            end
            
            openList = [openList; openList_new(maxIndex, :)];
            scoreList = [scoreList; scoreList_new(maxIndex)];
            removeIndex = true(1, size(openList_new, 1));
            removeIndex(maxIndex) = false;
            openList_new = openList_new(removeIndex, :);
            scoreList_new = scoreList_new(removeIndex);
        end
        
        openList = [openList; openList_new];
        scoreList = [scoreList; scoreList_new];
        find(best == 1)
        bestScore
        find(candidate == 1)
        candidateScore
        iterCount = iterCount + 1
    end
    
    selection = best;
end