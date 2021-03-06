% % The codes below will cluster tweets with time, hashtag and name-entity similarity
% %   using 4 different methods: spectral, hierarchical, K-means, kernel K-means clustering.
% % The 3 similarities are added with different weights to get the final similarity.
% % Grid search is applied to find the optimized weights.
% % Input files (similarity matrices) are output of C# codes.
% % Noise tweet topics havn't been deleted (686 real topics/clusters)
% root = '..\DataProcess\bin\Release\';
% label = load([root, 'label_cluster.txt']);
% timeSim = load([root, 'clusterHashtagSimilarity.txt']);
% hashtagSim = load([root, 'clusterTfIdfSimilarity.txt']);
% nameEntitySim = load([root, 'clusterNameEntitySetSimilarity.txt']);
% 
% fid=fopen('tfIdf\AdditionSimple_KmesansNormal.txt','w');
% fprintf(fid, 'clusterTimeSimilarity.txt\r\n');
% fprintf(fid, 'clusterHashtagSimilarity.txt\r\n');
% fprintf(fid, 'clusterNameEntitySetSimilarity.txt\r\n');
% fprintf(fid, '\r\n');
% 
% nmi_max = 0;
% for i = 0.0:0.1:1.0
%     for j = 0.0:0.1:1.0-i
%         k = 1.0 - i - j;
%         A = i * timeSim + j * hashtagSim + k * nameEntitySim;
%         A = sparse(A);
%         for d = 0:1:0
% %             % region Spectral Clustering
% %             [labelE] = sc(A, 0, 686 + d);
% %             % endregion Spectral Clustering
%             
% %             % region Hierarchical Clustering
% %             A = 1 - A;
% %             N = size(A, 1);
% %             B = ones(1, N * (N - 1) / 2);
% %             index = 1;
% %             for ii = 1:1:N-1
% %                 for jj = ii+1:1:N
% %                     B(index) = A(jj, ii);
% %                     index = index + 1;
% %                 end
% %             end
% %             Z = linkage(B, 'ward');
% %             labelE = cluster(Z, 'maxclust', 686 + d);
% %                 % mode: single, complete, average, weighted, ward
% %             % endregion Hierarchical Clustering
%             
%             % region Kmeans Clustering
%             labelE = k_means(A, 'random', 686);
%             % endregion Kmeans Clustering
% 
% %             % region Kernel Kmeans Clustering
% %             [labelE] = knKmeans(A, 686, @knGauss);
% %             labelE = labelE';
% %             % endregion Kernel Kmeans Clustering
% 
%             nmi_value = nmi(label, labelE);
%             if nmi_value > nmi_max
%                nmi_max = nmi_value;
%                record_i = i;
%                record_j = j;
%                record_k = k;
%                record_K = 686 + d;
%             end
%             fprintf(fid, '%.1f %.1f %.1f %d: %f\r\n', i, j, k, 686 + d, nmi_value);
%         end
%     end
% end
% 
% fprintf(fid, '\r\n');
% fprintf(fid, 'Max NMI:\r\n');
% fprintf(fid, '%.1f %.1f %.1f %d: %f\r\n', record_i, record_j, record_k, record_K, nmi_max);
% fclose(fid);




% % The codes below will cluster tweets with time, hashtag, name-entity, word jaccard, tf-idf and mention 
% %   similarity using 4 different methods: spectral, hierarchical, K-means, kernel K-means clustering.
% % The 6 similarities are added with different weights to get the final similarity.
% % Grid search is applied to find the optimized weights.
% % Input files (similarity matrices) are output of C# codes.
% % Noise tweet topics have been deleted (67 real topics/clusters)
% root = '..\DataProcess\bin\Release\NoNoise\';
% label = load([root, 'label_cluster.txt']);
% timeSim = load([root, 'clusterTimeSimilarity.txt']);
% hashtagSim = load([root, 'clusterHashtagSimilarity.txt']);
% nameEntitySim = load([root, 'clusterNameEntitySetSimilarity.txt']);
% jaccardSim = load([root, 'clusterWordJaccardSimilarity.txt']);
% tfIdfSim = load([root, 'clusterTfIdfSimilarity.txt']);
% mentionSim = load([root, 'clusterMentionSimilarity.txt']);
% 
% fid=fopen('combine_NoNoise\AdditionSimple_KernelKmeans.txt','w');
% fprintf(fid, 'clusterTimeSimilarity.txt\r\n');
% fprintf(fid, 'clusterHashtagSimilarity.txt\r\n');
% fprintf(fid, 'clusterNameEntitySetSimilarity.txt\r\n');
% fprintf(fid, 'clusterWordJaccardSimilarity.txt\r\n');
% fprintf(fid, 'clusterTfIdfSimilarity.txt\r\n');
% fprintf(fid, 'clusterMentionSimilarity.txt\r\n');
% fprintf(fid, '\r\n');
% 
% nmi_max = 0;
% for i = 0.0:0.1:1.01
%     for j = 0.0:0.1:1.01-i
%         for k = 0.0:0.1:1.01-i-j
%             for l = 0.0:0.1:1.01-i-j-k
%                 for m = 0.0:0.1:1.01-i-j-k-l
%                     n = 1.0-i-j-k-l-m;
%                     A = i * timeSim + j * hashtagSim + k * nameEntitySim + l * jaccardSim + m * tfIdfSim + n * mentionSim;
%                     A = sparse(A);
%                     for d = 0:1:0
% %                         % region Spectral Clustering
% %                         [labelE] = sc(A, 0, 67 + d);
% %                         % endregion Spectral Clustering
% 
% %                         % region Hierarchical Clustering
% %                         A = 1 - A;
% %                         N = size(A, 1);
% %                         B = ones(1, N * (N - 1) / 2);
% %                         index = 1;
% %                         for ii = 1:1:N-1
% %                             for jj = ii+1:1:N
% %                                 B(index) = A(jj, ii);
% %                                 index = index + 1;
% %                             end
% %                         end
% %                         Z = linkage(B, 'ward');
% %                         labelE = cluster(Z, 'maxclust', 67 + d);
% %                             % mode: single, complete, average, weighted, ward
% %                         % endregion Hierarchical Clustering
% 
% %                         % region Kmeans Clustering
% %                         labelE = k_means(A, 'random', 67 + d);
% %                         % endregion Kmeans Clustering
% 
%                         % region Kernel Kmeans Clustering
%                         [labelE] = knKmeans(A, 67 + d, @knGauss);
%                         labelE = labelE';
%                         % endregion Kernel Kmeans Clustering
% 
%                         nmi_value = nmi(label, labelE);
%                         if nmi_value > nmi_max
%                            nmi_max = nmi_value;
%                            record_i = i;
%                            record_j = j;
%                            record_k = k;
%                            record_l = l;
%                            record_m = m;
%                            record_n = n;
%                            record_K = 67 + d;
%                         end
%                         fprintf(fid, '%.1f %.1f %.1f %.1f %.1f %.1f %d: %f\r\n', i, j, k, l, m, n, 67 + d, nmi_value);
%                     end
%                 end
%             end
%         end
%     end
% end
% 
% fprintf(fid, '\r\n');
% fprintf(fid, 'Max NMI:\r\n');
% fprintf(fid, '%.1f %.1f %.1f %.1f %.1f %.1f %d: %f\r\n', record_i, record_j, record_k, record_l, record_m, record_n, record_K, nmi_max);
% fclose(fid);
% fclose('all');




% % The codes below will cluster tweets with semantic similarity with greedy and CM algorithm (java package SEMILAR)
% %   using 4 different methods: spectral, hierarchical, K-means, kernel K-means clustering.
% % This is one of the baseline in the experiment
% % Input files (similarity matrices) are output of C# codes.
% % Noise tweet topics have been deleted (67 real topics/clusters)
% root = '..\DataProcess\bin\Release\NoNoise\';
% label = load([root, 'label_cluster.txt']);
% A = load([root, 'clusterGreedySimilarity.txt']);
%    % "clusterCmSimilarity.txt" is another baseline file
% 
% fid=fopen('baseline_NoNoise\Greedy_KernelKmeans.txt','w');
% fprintf(fid, 'clusterGreedySimilarity.txt\r\n');
% fprintf(fid, '\r\n');
% 
% nmi_max = 0;
% for i = 1:1:100
%     for d = 0:1:0
% %         % region Spectral Clustering
% %         [labelE] = sc(A, 0, 67 + d);
% %         % endregion Spectral Clustering
% 
% %         % region Hierarchical Clustering
% %         A = 1 - A;
% %         N = size(A, 1);
% %         B = ones(1, N * (N - 1) / 2);
% %         index = 1;
% %         for ii = 1:1:N-1
% %             for jj = ii+1:1:N
% %                 B(index) = A(jj, ii);
% %                 index = index + 1;
% %             end
% %         end
% %         Z = linkage(B, 'ward');
% %         labelE = cluster(Z, 'maxclust', 67 + d);
% %             % mode: single, complete, average, weighted, ward
% %         % endregion Hierarchical Clustering
% 
% %         % region Kmeans Clustering
% %         labelE = k_means(A, 'random', 67 + d);
% %         % endregion Kmeans Clustering
% 
%         % region Kernel Kmeans Clustering
%         [labelE] = knKmeans(A, 67 + d, @knGauss);
%         labelE = labelE';
%         % endregion Kernel Kmeans Clustering
% 
%         nmi_value = nmi(label, labelE);
%         if nmi_value > nmi_max
%            nmi_max = nmi_value;
%            record_i = i;
%            record_K = 67 + d;
%         end
%         fprintf(fid, '%.1f %d: %f\r\n', i, 67 + d, nmi_value);
%     end
% end
% fprintf(fid, '\r\n');
% fprintf(fid, 'Max NMI:\r\n');
% fprintf(fid, '%.1f %d: %f\r\n', record_i, record_K, nmi_max);
% fclose(fid);
% fclose('all');




% % The codes below will cluster tweets with time, hashtag, name-entity, word jaccard, tf-idf and mention 
% %   similarity using 4 different methods: spectral, hierarchical, K-means, kernel K-means clustering.
% % The 6 similarities are added with different weights to get the final similarity.
% % Grid search is applied to find the optimized weights.
% % Input files (similarity matrices) are output of C# codes.
% % Noise tweet topics havn't been deleted (686 real topics/clusters)
% root = '..\DataProcess\bin\Release\';
% label = load([root, 'label_cluster.txt']);
% timeSim = load([root, 'clusterTimeSimilarity.txt']);
% hashtagSim = load([root, 'clusterHashtagSimilarity.txt']);
% nameEntitySim = load([root, 'clusterNameEntitySetSimilarity.txt']);
% jaccardSim = load([root, 'clusterWordJaccardSimilarity.txt']);
% tfIdfSim = load([root, 'clusterTfIdfSimilarity.txt']);
% mentionSim = load([root, 'clusterMentionSimilarity.txt']);
% 
% nmi_max = 0;
% for i = 0.0:0.1:1.01
%     for j = 0.0:0.1:1.01-i
%         for k = 0.0:0.1:1.01-i-j
%             for l = 0.0:0.1:1.01-i-j-k
%                 for m = 0.0:0.1:1.01-i-j-k-l
%                     n = 1.0-i-j-k-l-m;
%                     A = i * timeSim + j * hashtagSim + k * nameEntitySim + l * jaccardSim + m * tfIdfSim + n * mentionSim;
%                     A = sparse(A);
%                     for d = 0:1:0
% %                         % region Spectral Clustering
% %                         [labelE] = sc(A, 0, 686 + d);
% %                         % endregion Spectral Clustering
% % 
%                         % region Hierarchical Clustering
%                         A = 1 - A;
%                         N = size(A, 1);
%                         B = ones(1, N * (N - 1) / 2);
%                         index = 1;
%                         for ii = 1:1:N-1
%                             for jj = ii+1:1:N
%                                 B(index) = A(jj, ii);
%                                 index = index + 1;
%                             end
%                         end
%                         Z = linkage(B, 'average');
%                         labelE = cluster(Z, 'maxclust', 686 + d);
%                             % mode: single, complete, average, weighted, ward
%                         % endregion Hierarchical Clustering
% 
% %                         % region Kmeans Clustering
% %                         labelE = k_means(A, 'random', 686);
% %                         % endregion Kmeans Clustering
% 
% %                         % region Kernel Kmeans Clustering
% %                         [labelE] = knKmeans(A, 686, @knGauss);
% %                         labelE = labelE';
% %                         % endregion Kernel Kmeans Clustering
% 
%                         nmi_value = nmi(label, labelE);
%                         if nmi_value > nmi_max
%                             nmi_max = nmi_value;
%                             record_i = i;
%                             record_j = j;
%                             record_k = k;
%                             record_l = l;
%                             record_m = m;
%                             record_n = n;
%                             record_K = 686 + d;
%                             dlmwrite('combine\label_HierarchicalAverage.txt',labelE);
%                         end
%                     end
%                 end
%             end
%         end
%     end
% end




% % The codes below will cluster tweets with time, hashtag, name-entity, word jaccard, tf-idf and mention 
% %   similarity using 4 different methods: spectral, hierarchical, K-means, kernel K-means clustering.
% % 5 similarities are added with different weights and then multiplied by tf-idf to get the final similarity.
% % Input files (similarity matrices) are output of C# codes.
% % Noise tweet topics havn't been deleted (686 real topics/clusters)
% root = '..\DataProcess\bin\Release\';
% label = load([root, 'label_cluster.txt']);
% timeSim = load([root, 'clusterTimeSimilarity.txt']);
% hashtagSim = load([root, 'clusterHashtagSimilarity.txt']);
% nameEntitySim = load([root, 'clusterNameEntitySetSimilarity.txt']);
% jaccardSim = load([root, 'clusterWordJaccardSimilarity.txt']);
% tfIdfSim = load([root, 'clusterTfIdfSimilarity.txt']);
% mentionSim = load([root, 'clusterMentionSimilarity.txt']);
% 
% fid=fopen('new_test\MultiplySimple_HierarchicalAverage.txt','w');
% fprintf(fid, 'clusterTimeSimilarity.txt\r\n');
% fprintf(fid, 'clusterHashtagSimilarity.txt\r\n');
% fprintf(fid, 'clusterNameEntitySetSimilarity.txt\r\n');
% fprintf(fid, 'clusterWordJaccardSimilarity.txt\r\n');
% fprintf(fid, 'clusterTfIdfSimilarity.txt\r\n');
% fprintf(fid, 'clusterMentionSimilarity.txt\r\n');
% fprintf(fid, '\r\n');
% 
% nmi_max = 0;
% for i = 1.0:1.0:1.0
%     j = i;
%     k = i;
%     l = i;
%     m = i;
%     n = i;
%     A = (0.35 * timeSim + 0.1 * hashtagSim + 0.1 * nameEntitySim + 0.1 * mentionSim + 0.35 * jaccardSim) .* tfIdfSim;
%     A = sparse(A);
%     for d = 0:1:0
% %         % region Spectral Clustering
% %         [labelE] = sc(A, 0, 686 + d);
% %         % endregion Spectral Clustering
% 
% %         % region Hierarchical Clustering
% %         A = 1 - A;
% %         N = size(A, 1);
% %         B = ones(1, N * (N - 1) / 2);
% %         index = 1;
% %         for ii = 1:1:N-1
% %             for jj = ii+1:1:N
% %                 B(index) = A(jj, ii);
% %                 index = index + 1;
% %             end
% %         end
% %         Z = linkage(B, 'weighted');
% %         labelE = cluster(Z, 'maxclust', 686 + d);
% %             % mode: single, complete, average, weighted, ward
% %         % endregion Hierarchical Clustering
% 
% %         % region Kmeans Clustering
% %         labelE = k_means(A, 'random', 686);
% %         % endregion Kmeans Clustering
% 
%         % region Kernel Kmeans Clustering
%         [labelE] = knKmeans(A, 686, @knGauss);
%         labelE = labelE';
%         % endregion Kernel Kmeans Clustering
% 
%         nmi_value = nmi(label, labelE);
%         if nmi_value > nmi_max
%             nmi_max = nmi_value;
%             record_i = i;
%             record_j = j;
%             record_k = k;
%             record_l = l;
%             record_m = m;
%             record_n = n;
%             record_K = 686 + d;
%         end
%         fprintf(fid, '%.1f %.1f %.1f %.1f %.1f %.1f %d: %f\r\n', i, j, k, l, m, n, 686 + d, nmi_value);
%     end
% end
% 
% fprintf(fid, '\r\n');
% fprintf(fid, 'Max NMI:\r\n');
% fprintf(fid, '%.1f %.1f %.1f %.1f %.1f %.1f %d: %f\r\n', record_i, record_j, record_k, record_l, record_m, record_n, record_K, nmi_max);
% fclose(fid);
% fclose('all');




% The codes below are demo codes for filter/wrapper feature selection, as well as rumor classification and rumor ranking
label = load('label_clusterRumor.txt');
root = '..\DataProcess\bin\Release\Feature\';
fileNames = {'RatioOfSignal.txt', 'AvgCharLength_Signal.txt', 'AvgCharLength_All.txt', 'AvgCharLength_Ratio.txt', 'AvgWordLength_Signal.txt', 'AvgWordLength_All.txt', 'AvgWordLength_Ratio.txt', 'RtRatio_Signal.txt', 'RtRatio_All.txt', 'AvgUrlNum_Signal.txt', 'AvgUrlNum_All.txt', 'AvgHashtagNum_Signal.txt', 'AvgHashtagNum_All.txt', 'AvgMentionNum_Signal.txt', 'AvgMentionNum_All.txt', 'AvgRegisterTime_All.txt', 'AvgEclipseTime_All.txt', 'AvgFavouritesNum_All.txt', 'AvgFollwersNum_All.txt', 'AvgFriendsNum_All.txt', 'AvgReputation_All.txt', 'AvgTotalTweetNum_All.txt', 'AvgHasUrl_All.txt', 'AvgHasDescription_All.txt', 'AvgDescriptionCharLength_All.txt', 'AvgDescriptionWordLength_All.txt', 'AvgUtcOffset_All.txt', 'OpinionLeaderNum_All.txt', 'NormalUserNum_All.txt', 'OpinionLeaderRatio_All.txt', 'AvgQuestionMarkNum_All.txt', 'AvgExclamationMarkNum_All.txt', 'AvgUserRetweetNum_All.txt', 'AvgUserOriginalTweetNum_All.txt', 'AvgUserRetweetOriginalRatio_All.txt', 'AvgSentimentScore_All.txt', 'PositiveTweetRatio_All.txt', 'NegativeTweetRatio_All.txt', 'AvgPositiveWordNum_All.txt', 'AvgNegativeWordNum_All.txt', 'RetweetTreeRootNum_All.txt', 'RetweetTreeNonrootNum_All.txt', 'RetweetTreeMaxDepth_All.txt', 'RetweetTreeMaxBranchNum_All.txt', 'TotalTweetsCount_All.txt'};
features = cell(1, length(fileNames));
for i = 1:1:length(fileNames)
    features{i} = load([root, fileNames{i}]);
end
N = length(features);

% selection = Filter_corr(label, features, 15, "pearson", 1, "", 0);
% selection = Filter_nmi(label, features, 10, 10, 1, "continue", -0.3);
selection = Filter_relief(label, features, 10);
dlmwrite('selection_temp.txt', find(selection));
find(selection)

selection_index = [16,17,19,25,27,28,29,33,34,41];
selection = zeros(1, N);
selection(selection_index) = 1;
tic;
selection = Wrapper(label, features, 'NB', 'precision', 'float', selection);
toc;
dlmwrite('selection_temp1.txt', find(selection));
answer = find(selection);

% % Obsolete testing codes
% DT = fitctree(feature, label);
% f = inputFeature('featureCluster.txt');
% [L, score, node, cnum] = predict(DT, f);
% s = [score(:, 2) (0:13973)'];
% rank = sortrows(s, -1);
% dlmwrite('predict.txt', rank);
% 
% post = posterior(nb,test);

% Evaluation of a selected feature set (classification task)
disp('F1 score: ');
score = TrainAndTest(label, features, selection, 10, 'DT', 'F1');
disp(score);
disp('Average evluation: ');
result = EvaluateSelection(label, features, selection, 10);
disp(result);
disp('Average evluation: ');
result = Evaluation(selection_index);
disp(result);
disp('Average evluation of multi-classifiers voting: ');
result = EvaluateVote(label, features, 10);
disp(result);

% Find out the most possible rumors with a selected feature set (ranking task)
ranking = PredictAndRank(selection_index, 'NB');
disp('Ranking completed (predict.txt)');
PredictAndVote();
disp('Ranking by multi-classifiers voting completed (predict_DT.txt, predict_NB.txt, predict_All.txt)');





% % The codes below delete noise tweet topics and create new label and similarity files  
% root = '..\DataProcess\bin\Release\';
% oriClSelected_filter = load([root, 'label_oriClusterSelected.txt']);
% oriClSelected_index = find(oriClSelected_filter);
% label = load([root, 'label_cluster.txt']);
% timeSim = load([root, 'clusterTimeSimilarity.txt']);
% hashtagSim = load([root, 'clusterHashtagSimilarity.txt']);
% nameEntitySim = load([root, 'clusterNameEntitySetSimilarity.txt']);
% jaccardSim = load([root, 'clusterWordJaccardSimilarity.txt']);
% tfIdfSim = load([root, 'clusterTfIdfSimilarity.txt']);
% mentionSim = load([root, 'clusterMentionSimilarity.txt']);
% cmSim = load([root, 'clusterCmSimilarity.txt']);
% greedySim = load([root, 'clusterGreedySimilarity.txt']);
% 
% label = label(oriClSelected_index);
% timeSim = timeSim(oriClSelected_index, oriClSelected_index);
% hashtagSim = hashtagSim(oriClSelected_index, oriClSelected_index);
% nameEntitySim = nameEntitySim(oriClSelected_index, oriClSelected_index);
% jaccardSim = jaccardSim(oriClSelected_index, oriClSelected_index);
% tfIdfSim = tfIdfSim(oriClSelected_index, oriClSelected_index);
% mentionSim = mentionSim(oriClSelected_index, oriClSelected_index);
% cmSim = cmSim(oriClSelected_index, oriClSelected_index);
% greedySim = greedySim(oriClSelected_index, oriClSelected_index);
% 
% root = '..\DataProcess\bin\Release\NoNoise\';
% dlmwrite([root, 'label_cluster.txt'], label);
% dlmwrite([root, 'clusterTimeSimilarity.txt'], timeSim);
% dlmwrite([root, 'clusterHashtagSimilarity.txt'], hashtagSim);
% dlmwrite([root, 'clusterNameEntitySetSimilarity.txt'], nameEntitySim);
% dlmwrite([root, 'clusterWordJaccardSimilarity.txt'], jaccardSim);
% dlmwrite([root, 'clusterTfIdfSimilarity.txt'], tfIdfSim);
% dlmwrite([root, 'clusterMentionSimilarity.txt'], mentionSim);
% dlmwrite([root, 'clusterCmSimilarity.txt'], cmSim);
% dlmwrite([root, 'clusterGreedySimilarity.txt'], greedySim);



% % The codes below will cluster tweets with time, hashtag, name-entity, word jaccard, tf-idf and mention 
% %   similarity using 4 different methods: spectral, hierarchical, K-means, kernel K-means clustering.
% % The 6 similarities are added with different weights to get the final similarity.
% % The weights (0.125, 0.15, 0.025, 0.1, 0.5875, 0.0125) are average values from optimized weights 
% %   using different clustering methods.
% % Input files (similarity matrices) are output of C# codes.
% % Noise tweet topics have been deleted (67 real topics/clusters)
% root = '..\DataProcess\bin\Release\NoNoise\';
% label = load([root, 'label_cluster.txt']);
% timeSim = load([root, 'clusterTimeSimilarity.txt']);
% hashtagSim = load([root, 'clusterHashtagSimilarity.txt']);
% nameEntitySim = load([root, 'clusterNameEntitySetSimilarity.txt']);
% jaccardSim = load([root, 'clusterWordJaccardSimilarity.txt']);
% tfIdfSim = load([root, 'clusterTfIdfSimilarity.txt']);
% mentionSim = load([root, 'clusterMentionSimilarity.txt']);
% 
% fid=fopen('Avg_Weight\Avg_Weighted_B.txt','w');
% fprintf(fid, 'clusterTimeSimilarity.txt: 0.125\r\n');
% fprintf(fid, 'clusterHashtagSimilarity.txt: 0.15\r\n');
% fprintf(fid, 'clusterNameEntitySetSimilarity.txt: 0.025\r\n');
% fprintf(fid, 'clusterWordJaccardSimilarity.txt: 0.1\r\n');
% fprintf(fid, 'clusterTfIdfSimilarity.txt: 0.5875\r\n');
% fprintf(fid, 'clusterMentionSimilarity.txt: 0.0125\r\n');
% fprintf(fid, '\r\n');
% 
% 
% A = 0.125 * timeSim + 0.15 * hashtagSim + 0.025 * nameEntitySim + 0.1 * jaccardSim + 0.5875 * tfIdfSim + 0.0125 * mentionSim;
% A = sparse(A);
% for d = 0:1:0
%     % region Spectral Clustering
%     [labelE] = sc(A, 0, 67 + d);
%     nmi_value = nmi(label, labelE);
%     fprintf(fid, 'Spectral: %f\r\n', nmi_value);
%     % endregion Spectral Clustering
% 
%     % region Hierarchical Clustering
%     A = 1 - A;
%     N = size(A, 1);
%     B = ones(1, N * (N - 1) / 2);
%     index = 1;
%     for ii = 1:1:N-1
%         for jj = ii+1:1:N
%             B(index) = A(jj, ii);
%             index = index + 1;
%         end
%     end
%     
%     Z = linkage(B, 'single');
%     labelE = cluster(Z, 'maxclust', 67 + d);
%     nmi_value = nmi(label, labelE);
%     fprintf(fid, 'H-Sgl: %f\r\n', nmi_value);
%     
%     Z = linkage(B, 'complete');
%     labelE = cluster(Z, 'maxclust', 67 + d);
%     nmi_value = nmi(label, labelE);
%     fprintf(fid, 'H-Cpl: %f\r\n', nmi_value);
%     
%     Z = linkage(B, 'average');
%     labelE = cluster(Z, 'maxclust', 67 + d);
%     nmi_value = nmi(label, labelE);
%     fprintf(fid, 'H-Avg: %f\r\n', nmi_value);
%     
%     Z = linkage(B, 'weighted');
%     labelE = cluster(Z, 'maxclust', 67 + d);
%     nmi_value = nmi(label, labelE);
%     fprintf(fid, 'H-Wtd: %f\r\n', nmi_value);
%     
%     Z = linkage(B, 'ward');
%     labelE = cluster(Z, 'maxclust', 67 + d);
%     nmi_value = nmi(label, labelE);
%     fprintf(fid, 'H-Wrd: %f\r\n', nmi_value);
%     % endregion Hierarchical Clustering
% 
%     % region Kmeans Clustering
%     labelE = k_means(A, 'random', 67 + d);
%     nmi_value = nmi(label, labelE);
%     fprintf(fid, 'Kmeans: %f\r\n', nmi_value);
%     % endregion Kmeans Clustering
% 
%     % region Kernel Kmeans Clustering
%     [labelE] = knKmeans(A, 67 + d, @knGauss);
%     labelE = labelE';
%     nmi_value = nmi(label, labelE);
%     fprintf(fid, 'Kn-Km: %f\r\n', nmi_value);
%     % endregion Kernel Kmeans Clustering
% end
% 
% fclose(fid);
% fclose('all');


% % The codes below will cluster tweets with time, hashtag, name-entity, word jaccard, tf-idf and mention 
% %   similarity using 4 different methods: spectral, hierarchical, K-means, kernel K-means clustering.
% % Grid search is applied to find how the clusters number K has an influence on clustering effect.
% % The 6 similarities are added with different weights to get the final similarity.
% % The weights (0.125, 0.15, 0.025, 0.1, 0.5875, 0.0125) are average values from optimized weights 
% %   using different clustering methods.
% % Input files (similarity matrices) are output of C# codes.
% % Noise tweet topics have been deleted (67 real topics/clusters)
% root = '..\DataProcess\bin\Release\NoNoise\';
% label = load([root, 'label_cluster.txt']);
% timeSim = load([root, 'clusterTimeSimilarity.txt']);
% hashtagSim = load([root, 'clusterHashtagSimilarity.txt']);
% nameEntitySim = load([root, 'clusterNameEntitySetSimilarity.txt']);
% jaccardSim = load([root, 'clusterWordJaccardSimilarity.txt']);
% tfIdfSim = load([root, 'clusterTfIdfSimilarity.txt']);
% mentionSim = load([root, 'clusterMentionSimilarity.txt']);
% 
% fid=fopen('Avg_Weight\K_NMI_B_Ward.txt','w');
% fprintf(fid, 'clusterTimeSimilarity.txt: 0.125\r\n');
% fprintf(fid, 'clusterHashtagSimilarity.txt: 0.15\r\n');
% fprintf(fid, 'clusterNameEntitySetSimilarity.txt: 0.025\r\n');
% fprintf(fid, 'clusterWordJaccardSimilarity.txt: 0.1\r\n');
% fprintf(fid, 'clusterTfIdfSimilarity.txt: 0.5875\r\n');
% fprintf(fid, 'clusterMentionSimilarity.txt: 0.0125\r\n');
% fprintf(fid, '\r\n');
% 
% 
% A = 0.125 * timeSim + 0.15 * hashtagSim + 0.025 * nameEntitySim + 0.1 * jaccardSim + 0.5875 * tfIdfSim + 0.0125 * mentionSim;
% A = sparse(A);
% A = 1 - A;
% N = size(A, 1);
% B = ones(1, N * (N - 1) / 2);
% index = 1;
% for ii = 1:1:N-1
%     for jj = ii+1:1:N
%         B(index) = A(jj, ii);
%         index = index + 1;
%     end
% end
% offsets = [-37,-27,-17,-7,0,3,13,23,28,33,38,43,48,53,58,63];
% for d = 1:1:length(offsets)
%     % region Hierarchical Clustering
% %     Z = linkage(B, 'single');
% %     labelE = cluster(Z, 'maxclust', 67 + offsets(d));
% %     nmi_value = nmi(label, labelE);
% %     fprintf(fid, 'H-Sgl, %d: %f\r\n', 67 + offsets(d), nmi_value);
%     
% %     Z = linkage(B, 'complete');
% %     labelE = cluster(Z, 'maxclust', 67 + offsets(d));
% %     nmi_value = nmi(label, labelE);
% %     fprintf(fid, 'H-Cpl, %d: %f\r\n', 67 + offsets(d), nmi_value);
%     
% %     Z = linkage(B, 'average');
% %     labelE = cluster(Z, 'maxclust', 67 + offsets(d));
% %     nmi_value = nmi(label, labelE);
% %     fprintf(fid, 'H-Avg, %d: %f\r\n', 67 + offsets(d), nmi_value);
%     
% %     Z = linkage(B, 'weighted');
% %     labelE = cluster(Z, 'maxclust', 67 + offsets(d));
% %     nmi_value = nmi(label, labelE);
% %     fprintf(fid, 'H-Wtd, %d: %f\r\n', 67 + offsets(d), nmi_value);
%     
%     Z = linkage(B, 'ward');
%     labelE = cluster(Z, 'maxclust', 67 + offsets(d));
%     nmi_value = nmi(label, labelE);
%     fprintf(fid, 'H-Wrd, %d: %f\r\n', 67 + offsets(d), nmi_value);
%     % endregion Hierarchical Clustering
% end
% 
% fclose(fid);
% fclose('all');