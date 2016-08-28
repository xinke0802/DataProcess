function result = Evaluation(selection_index)
% Evaluate one selected feature set with 10-fold cross-validation using decision tree and naive bayesian
%   under 3 different evaluations: F1 measure, accuracy and precision
% Output:
%   evaluation_temp.txt: [meanAcc_DT meanF1_DT meanPrecision_DT stdAcc_DT stdF1_DT stdPrecision_DT;
%                         meanAcc_NB meanF1_NB meanPrecision_NB stdAcc_NB stdF1_NB stdPrecision_NB]
%                        2 x 6 matrix (std - standard deviation ; Acc - accuracy; DT - decision tree; NB - naive bayesian)

    label = load('label_clusterRumor.txt');
    root = '..\DataProcess\bin\Release\Feature\';
    fileNames = {'RatioOfSignal.txt', 'AvgCharLength_Signal.txt', 'AvgCharLength_All.txt', 'AvgCharLength_Ratio.txt', 'AvgWordLength_Signal.txt', 'AvgWordLength_All.txt', 'AvgWordLength_Ratio.txt', 'RtRatio_Signal.txt', 'RtRatio_All.txt', 'AvgUrlNum_Signal.txt', 'AvgUrlNum_All.txt', 'AvgHashtagNum_Signal.txt', 'AvgHashtagNum_All.txt', 'AvgMentionNum_Signal.txt', 'AvgMentionNum_All.txt', 'AvgRegisterTime_All.txt', 'AvgEclipseTime_All.txt', 'AvgFavouritesNum_All.txt', 'AvgFollwersNum_All.txt', 'AvgFriendsNum_All.txt', 'AvgReputation_All.txt', 'AvgTotalTweetNum_All.txt', 'AvgHasUrl_All.txt', 'AvgHasDescription_All.txt', 'AvgDescriptionCharLength_All.txt', 'AvgDescriptionWordLength_All.txt', 'AvgUtcOffset_All.txt', 'OpinionLeaderNum_All.txt', 'NormalUserNum_All.txt', 'OpinionLeaderRatio_All.txt', 'AvgQuestionMarkNum_All.txt', 'AvgExclamationMarkNum_All.txt', 'AvgUserRetweetNum_All.txt', 'AvgUserOriginalTweetNum_All.txt', 'AvgUserRetweetOriginalRatio_All.txt', 'AvgSentimentScore_All.txt', 'PositiveTweetRatio_All.txt', 'NegativeTweetRatio_All.txt', 'AvgPositiveWordNum_All.txt', 'AvgNegativeWordNum_All.txt', 'RetweetTreeRootNum_All.txt', 'RetweetTreeNonrootNum_All.txt', 'RetweetTreeMaxDepth_All.txt', 'RetweetTreeMaxBranchNum_All.txt', 'TotalTweetsCount_All.txt'};
    features = cell(1, length(fileNames));
    for i = 1:1:length(fileNames)
        features{i} = load([root, fileNames{i}]);
    end
    N = length(features);
    
    if isempty(selection_index)
        result = EvaluateVote(label, features, 10);
    else
        selection = zeros(1, N);
        selection(selection_index) = 1;
        result = EvaluateSelection(label, features, selection, 10);
    end
    
    dlmwrite('evaluation_temp.txt', result, 'delimiter', ' ');
end