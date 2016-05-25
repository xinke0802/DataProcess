function selection = Wrapper(label, features, classifier, eval)
    N = length(features);
    
    openList = zeros(1, N);
    scoreList = zeros(1, 1);
    closedList = [];
    best = [];
    bestScore = 0;
    iterCount = 0;
    iterNoChange = 0;
    
    while iterNoChange < 100 && iterCount < 6000 && length(find(best == 1)) < 15 && size(openList, 1) ~= 0
        maxIndex = find(scoreList == max(scoreList));
        candidateIndex = maxIndex(1);
        candidate = openList(candidateIndex, :);
        candidateScore = scoreList(candidateIndex);
        
        removeIndex = true(1, size(openList, 1));
        removeIndex(candidateIndex) = false;
        openList = openList(removeIndex, :);
        scoreList = scoreList(removeIndex);
        closedList = [closedList; candidate];
        
        if candidateScore - 1e-10 > bestScore
            best = candidate;
            bestScore = candidateScore;
            iterNoChange = 0;
        elseif isequal(best, []) == 0
            iterNoChange = iterNoChange + 1;
        end
        
        openList_new = [];
        scoreList_new = [];
        for i = 1:1:N
            if candidate(i) == 1
                open_new = candidate;
                open_new(i) = 0;
            else
                open_new = candidate;
                open_new(i) = 1;
            end
            if (isequal(openList, []) == 0 && ismember(open_new, openList, 'rows')) || (isequal(closedList, []) == 0 && ismember(open_new, closedList, 'rows'))
                continue;
            end
            score = TrainAndTest(label, features, open_new, 5, classifier, eval);
            openList_new = [openList_new; open_new];
            scoreList_new = [scoreList_new; score];
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
            
            if (isequal(openList, []) == 0 && ismember(open_new, openList, 'rows')) || (isequal(closedList, []) == 0 && ismember(open_new, closedList, 'rows'))
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