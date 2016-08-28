function selection = Filter_corr(label, features, n, eval, w, mode, threshold)
% Correlation based filtter feature selection
% Input:
%   label: m x 1 label vector (m is the number of samples)
%   features: m x s feature matrix (s is the number of different types of features)
%   n: The number of features that require to be selected
%   eval: The correlation function: "spearman" or "pearson"
%   w: The weight of non-correlation of selected features to each other
%   mode: "continue" - when n features are selected, continue to select features
%                      until correlation evaluation value is below threshold
%          default   - when n features are selected, stop feature selection
%   threshold: stop threshold in continue mode
% Output:
%   selection: 1 x N binary vector (N is the number of features): 1 means corresponding
%              feature is selected; 0 means corresonding feature is not selected

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