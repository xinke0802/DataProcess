function selection = Filter_corr(label, features, n, eval, w, mode, threshold)
    N = length(features);
    selection = zeros(1, N);
    
    if strcmp(eval, 'spearman') == 1
        str = 'spearman';
    else
        str = 'pearson';
    end
    
    for i = 1:1:n
        maxIndex = -1;
        maxS = -100;
        for j = 1:1:N
            if selection(j) == 1
                continue;
            end
            S = abs(corr(features{j}, label, 'type', str));
            if w ~= 0
                for k = 1:1:N
                    if selection(k) == 0
                        continue;
                    end
                    S = S - w * abs(corr(features{j}, features{k}, 'type', str)) / length(find(selection));
                end
            end
            if S > maxS
                maxS = S;
                maxIndex = j;
            end
        end
        selection(maxIndex) = 1;
    end
    
    while strcmp(mode, 'continue') == 1 && length(find(selection)) ~= N
        maxIndex = -1;
        maxS = -100;
        for j = 1:1:N
            if selection(j) == 1
                continue;
            end
            S = abs(corr(features{j}, label, 'type', str));
            if w ~= 0
                for k = 1:1:N
                    if selection(k) == 0
                        continue;
                    end
                    S = S - w * abs(corr(features{j}, features{k}, 'type', str)) / length(find(selection));
                end
            end
            if S > maxS
                maxS = S;
                maxIndex = j;
            end
        end
        if maxS < threshold
            break;
        end
        selection(maxIndex) = 1;
    end
end