function PredictAndVote()

    DT_SI = cell(1, 6);
    NB_SI = cell(1, 6);
    
    DT_SI{1} = [12,14,20,27,31,32,33,34,39,40];
    DT_SI{2} = [2,7,13,15,18,20,21,29,31,32,33,34,37,39,40];
    DT_SI{3} = [5,6,8,9,12,13,16,17,19,25,26,27,28,29,33,34,35,38,41,43];
    DT_SI{4} = [8,9,10,11,14,27,32,39,42,44];
    DT_SI{5} = [5,6,12,22,23,24,28,31,34,43];
    DT_SI{6} = [6,12,17,20,26,27,28,33,34,35,37,38,40,42];
    
    NB_SI{1} = [2,3,12,13,14,15,22,27,31,33,34,35,36,37,39,40,41,42,44,45];
    NB_SI{2} = [19,20,21,29,31,34,37,39,41,44];
    NB_SI{3} = [5,6,13,16,17,19,25,26,27,28,29,33,34,41,43];
    NB_SI{4} = [1,2,8,9,17,23,24,25,26,34,35,36,37,39,43];
    NB_SI{5} = [1,6,8,9,10,11,14,16,17,18,20,21,23,24,25,26,27,30,32,34,35,38,43];
    NB_SI{6} = [6,11,14,15,18,22,25,26,29,31,33,34,35,36,39,40,41];

    rank_All = [];
    
    rank_DT = [];
    for i = 1:1:6
        selection_index = DT_SI{i};
        rank_i = PredictAndRank(selection_index, 'DT');
        rank_i(find(isnan(rank_i(:, 1))), :) = [];
        for j = 1:1:200
            id = rank_i(j, 2);
            if isempty(rank_DT)
                rank_DT = [1, id];
            else
                target = find(rank_DT(:, 2) == id);
                if length(target) == 0
                    rank_DT = [rank_DT; 1, id];
                else
                    rank_DT(target(1), 1) = rank_DT(target(1), 1) + 1;
                end
            end
            if isempty(rank_All)
                rank_All = [1, id];
            else
                target = find(rank_All(:, 2) == id);
                if length(target) == 0
                    rank_All = [rank_All; 1, id];
                else
                    rank_All(target(1), 1) = rank_All(target(1), 1) + 1;
                end
            end
        end
    end
    rank_DT = sortrows(rank_DT, [-1 2]);
    
    rank_NB = [];
    for i = 1:1:6
        selection_index = NB_SI{i};
        rank_i = PredictAndRank(selection_index, 'NB');
        rank_i(find(isnan(rank_i(:, 1))), :) = [];
        for j = 1:1:200
            id = rank_i(j, 2);
            if isempty(rank_NB)
                rank_NB = [1, id];
            else
                target = find(rank_NB(:, 2) == id);
                if length(target) == 0
                    rank_NB = [rank_NB; 1, id];
                else
                    rank_NB(target(1), 1) = rank_NB(target(1), 1) + 1;
                end
            end
            if isempty(rank_All)
                rank_All = [1, id];
            else
                target = find(rank_All(:, 2) == id);
                if length(target) == 0
                    rank_All = [rank_All; 1, id];
                else
                    rank_All(target(1), 1) = rank_All(target(1), 1) + 1;
                end
            end
        end
    end
    rank_NB = sortrows(rank_NB, [-1 2]);
    
    rank_All = sortrows(rank_All, [-1 2]);
    
    dlmwrite('predict_DT.txt', rank_DT);
    dlmwrite('predict_NB.txt', rank_NB);
    dlmwrite('predict_All.txt', rank_All);
end