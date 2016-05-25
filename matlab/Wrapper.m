function selection = Wrapper(label, features, classifier, eval)
    N = length(features);
    
    openList = zeros(1, N);
    evalList = zeros(1, N);
    closedList = [];
    bestIndex = 1;
    best = zeros(1, N);
    iterCount = 0;
    iterNoChange = 0;
    
    while iterNoChange < 10 && iterCount < 500
        openList_new = [];
        for i = 1:1:N
            if best(i) == 1
                open_new = best;
                open_new(i) = 0;
            else
                open_new = best;
                open_new(i) = 1;
            end
            if ismember(open_new, openList, 'rows') || ismember(open_new, closedList, 'rows')
                continue;
            end
            score = TrainAndTest(label, features, open_new, 5, classifier, eval);
            
        end
    end
end