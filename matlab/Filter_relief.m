function selection = Filter_relief(label, features, n)
% Relief filter feature selection
% Input:
%   label: m x 1 label vector (m is the number of samples)
%   features: m x s feature matrix (s is the number of different types of features)
%   n: The number of features that require to be selected
% Output:
%   selection: 1 x N binary vector (N is the number of features): 1 means corresponding
%              feature is selected; 0 means corresonding feature is not selected

    N = length(features);
    m = length(label);
    
    minV = zeros(1, N);
    rangeV = zeros(1, N);
    for i = 1:1:N
        minV(i) = min(features{i});
        rangeV(i) = range(features{i});
    end
    
    w = zeros(1, N);
    
    for i = 1:1:m
        disArr = zeros(m, 1);
        disMat = zeros(m, N);
        for j = 1:1:m
            dis = 0;
            for k = 1:1:N
                delta = features{k}(i) - features{k}(j);
                delta_2 = delta * delta;
                disMat(j, k) = delta_2;
                dis = dis + delta_2;
            end
            disArr(j) = dis;
        end
        
        % Find Near-hit sample
        minIndex = -1;
        minDis = 0;
        for j = 1:1:m
            if i == j
                continue;
            elseif label(i) ~= label(j)
                continue;
            elseif minIndex == -1
                minIndex = j;
                minDis = disArr(j);
                continue;
            end
            if disArr(j) < minDis
                minIndex = j;
                minDis = disArr(j);
            end
        end
        w = w - disMat(minIndex, :);
        
        % Find Near-miss sample
        minIndex = -1;
        minDis = 0;
        for j = 1:1:m
            if i == j
                continue;
            elseif label(i) == label(j)
                continue;
            elseif minIndex == -1
                minIndex = j;
                minDis = disArr(j);
                continue;
            end
            if disArr(j) < minDis
                minIndex = j;
                minDis = disArr(j);
            end
        end
        w = w + disMat(minIndex, :);
    end
    
    % Find top n relevant features
    w = w / m;
    selection = zeros(1, N);
    for i = 1:1:n
        maxIndex = -1;
        maxW = -100;
        for j = 1:1:N
            if selection(j) == 1
                continue;
            end
            if maxIndex == -1
                maxIndex = j;
                maxW = w(j);
                continue;
            end
            if w(j) > maxW
                maxIndex = j;
                maxW = w(j);
            end
        end
        selection(maxIndex) = 1;
    end
end