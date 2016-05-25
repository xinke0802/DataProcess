function selection = Filter_nmi(label, features, n, clNum, w, mode, threshold)
    N = length(features);
    selection = zeros(1, N);
    label = label + 1;

    for i = 1:1:N
        if length(unique(features{i})) <= clNum
            features{i} = features{i} + 1;
            continue;
        end
        minV = min(features{i});
        interval = range(features{i}) / clNum;
        for j = 1:1:length(features{i})
            value = floor((features{i}(j) - minV) / interval) + 1;
            if value > clNum
                value = clNum;
            elseif value < 1
                value = 1;
            elseif isnan(value)
                value = 1;
            end
            features{i}(j) = value;
        end
        rng = unique(features{i});
        for j = 1:1:length(features{i})
            mark = find(rng == features{i}(j));
            features{i}(j) = mark(1);
        end
    end
    
    for i = 1:1:n
        maxIndex = -1;
        maxS = -100;
        for j = 1:1:N
            if selection(j) == 1
                continue;
            end
            S = nmi(features{j}, label);
            if w ~= 0
                for k = 1:1:N
                    if selection(k) == 0
                        continue;
                    end
                    S = S - w * nmi(features{j}, features{k}) / length(find(selection));
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
            S = nmi(features{j}, label);
            if w ~= 0
                for k = 1:1:N
                    if selection(k) == 0
                        continue;
                    end
                    S = S - w * nmi(features{j}, features{k}) / length(find(selection));
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